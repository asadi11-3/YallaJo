using Auth.Application.Commands.ForgotPassword;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Events;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Abstractions;


namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2C-3 — the handler no longer sends SMTP inline. It persists a
/// <see cref="PasswordResetToken"/> in Issued/Pending and raises a
/// <see cref="PasswordResetTokenIssuedEvent"/> on the aggregate so the
/// downstream outbox pipeline can dispatch the email asynchronously.
/// <para>
/// SMTP-failure coverage moves to
/// <c>PasswordResetEmailDispatchHandlerTests</c>. Enumeration-safety
/// and throttle invariants are preserved verbatim from Phase 2C-2.
/// </para>
/// </summary>
public sealed class ForgotPasswordCommandHandlerTests
{
    private const string GenericMessage = "If this email exists, a reset code was sent.";

    private readonly ISecurityService              _security   = Substitute.For<ISecurityService>();
    private readonly IPasswordResetTokenRepository _tokenRepo  = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IAuthUnitOfWork               _uow        = Substitute.For<IAuthUnitOfWork>();
    private readonly IOtpService                   _otpService = Substitute.For<IOtpService>();

    private ForgotPasswordCommandHandler CreateSut() =>
        new(_security, _tokenRepo, _uow, _otpService,
            NullLogger<ForgotPasswordCommandHandler>.Instance);

    private static ForgotPasswordCommand Command(string email = "user@example.com")
        => new(email, "test-recaptcha-token");

    private void StubActiveVerifiedAccount(Guid userId, string email = "user@example.com")
    {
        _security.GetAccountStatusByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AccountStatus(
                userId,
                email,
                IsActive: true,
                IsEmailVerified: true,
                Lifecycle: AccountLifecycleSnapshot.Active));
    }

    private void StubNoRecentOrActiveTokens()
    {
        _tokenRepo.GetLatestActiveForUserReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);
        _tokenRepo.GetActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PasswordResetToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericSuccess_AndSkipWrites_WhenUserDoesNotExist()
    {
        _security.GetAccountStatusByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((AccountStatus?)null);

        var sut = CreateSut();

        var result = await sut.Handle(Command("ghost@example.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be(GenericMessage);

        await _tokenRepo.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, true,  AccountLifecycleSnapshot.PendingActivation)]
    [InlineData(false, true,  AccountLifecycleSnapshot.Suspended)]
    [InlineData(true,  false, AccountLifecycleSnapshot.Active)]
    [InlineData(false, false, AccountLifecycleSnapshot.Provisioned)]
    [InlineData(false, true,  AccountLifecycleSnapshot.Archived)]
    public async Task Handle_ShouldReturnGenericSuccess_AndSkipWrites_WhenAccountIsNotEligible(
        bool isActive, bool isEmailVerified, AccountLifecycleSnapshot lifecycle)
    {
        var userId = Guid.NewGuid();
        _security.GetAccountStatusByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AccountStatus(userId, "user@example.com", isActive, isEmailVerified, lifecycle));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be(GenericMessage);

        await _tokenRepo.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPersistTokenInIssuedPending_AndRaiseDomainEvent_OnHappyPath()
    {
        var userId = Guid.NewGuid();
        StubActiveVerifiedAccount(userId);
        StubNoRecentOrActiveTokens();

        _otpService.Generate().Returns("909090");
        _otpService.Hash(Arg.Any<string>()).Returns("hash");

        PasswordResetToken? persisted = null;
        _tokenRepo.AddAsync(Arg.Do<PasswordResetToken>(t => persisted = t), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be(GenericMessage);

        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be(userId);
        persisted.TokenHash.Should().Be("hash");
        persisted.DeliveryAddress.Should().Be("user@example.com");
        persisted.ResetOrigin.Should().Be(PasswordResetOrigin.SelfService);

        // Critical Phase 2C-3 invariant: the token is persisted in
        // Issued/Pending — NOT marked Delivered inline. The
        // PasswordResetEmailDispatchHandler flips it to Delivered only
        // after SMTP succeeds.
        persisted.State.Should().Be(PasswordResetTokenState.Issued);
        persisted.DeliveryStatus.Should().Be(PasswordResetTokenDeliveryStatus.Pending);
        persisted.LastSentAt.Should().BeNull();

        // Domain event carries the plain code for the downstream
        // dispatcher (the aggregate only stores the hash).
        var domainEvent = persisted.DomainEvents
            .OfType<PasswordResetTokenIssuedEvent>()
            .Single();
        domainEvent.TokenId.Should().Be(persisted.Id);
        domainEvent.UserId.Should().Be(userId);
        domainEvent.DeliveryAddress.Should().Be("user@example.com");
        domainEvent.PlainCode.Should().Be("909090");
        domainEvent.ExpiresAt.Should().Be(persisted.ExpiresAt);
        domainEvent.Origin.Should().Be(PasswordResetOrigin.SelfService);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrottle_WhenRecentTokenIsWithin60s_ReturningGenericSuccess()
    {
        var userId = Guid.NewGuid();
        StubActiveVerifiedAccount(userId);

        var recent = PasswordResetToken.Issue(userId, "hash", "user@example.com", 10);
        _tokenRepo.GetLatestActiveForUserReadOnlyAsync(userId, Arg.Any<CancellationToken>())
            .Returns(recent);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be(GenericMessage);

        await _tokenRepo.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSupersedePriorActiveTokens_BeforeIssuingNewOne()
    {
        var userId = Guid.NewGuid();
        StubActiveVerifiedAccount(userId);

        var old1 = PasswordResetToken.Issue(userId, "h1", "user@example.com", 10);
        var old2 = PasswordResetToken.Issue(userId, "h2", "user@example.com", 10);
        old2.MarkDelivered();

        // Backdate CreatedAt on old1 beyond the 60s throttle window so
        // the handler proceeds past the throttle check.
        typeof(YallaJo.SharedKernel.Domain.Entities.BaseEntity<Guid>)
            .GetProperty("CreatedAt")!
            .GetSetMethod(nonPublic: true)!
            .Invoke(old1, new object[] { DateTime.UtcNow.AddMinutes(-5) });

        _tokenRepo.GetLatestActiveForUserReadOnlyAsync(userId, Arg.Any<CancellationToken>())
            .Returns(old1);
        _tokenRepo.GetActiveForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { old1, old2 });

        _otpService.Generate().Returns("909090");
        _otpService.Hash(Arg.Any<string>()).Returns("hash");

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        old1.State.Should().Be(PasswordResetTokenState.Revoked);
        old1.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.Superseded);
        old2.State.Should().Be(PasswordResetTokenState.Revoked);
        old2.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.Superseded);
    }
}
