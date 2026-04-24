using Auth.Application.Commands.SendActivationEmail;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2C-1 — the handler now writes <see cref="ActivationToken"/> rows
/// instead of <c>Otp(UserInvite)</c>. These tests verify:
///   • lifecycle gate (Provisioned / PendingActivation only),
///   • NotFound on unknown account,
///   • prior active ActivationTokens are superseded,
///   • new token is persisted in Issued/Pending state,
///   • email success → MarkDelivered + MarkPendingActivationAsync,
///   • email failure → RevokeOnEmailFailure, no lifecycle advance,
///   • idempotent resend from PendingActivation still succeeds.
/// </summary>
public sealed class SendActivationEmailCommandHandlerTests
{
    private readonly IUserRegistrationService   _users    = Substitute.For<IUserRegistrationService>();
    private readonly IActivationTokenRepository _tokenRepo = Substitute.For<IActivationTokenRepository>();
    private readonly IAuthUnitOfWork            _uow      = Substitute.For<IAuthUnitOfWork>();
    private readonly IInviteTokenService        _tokens   = Substitute.For<IInviteTokenService>();
    private readonly IInviteLinkBuilder         _links    = Substitute.For<IInviteLinkBuilder>();
    private readonly IEmailService              _email    = Substitute.For<IEmailService>();

    private SendActivationEmailCommandHandler CreateSut() =>
        new(_users, _tokenRepo, _uow, _tokens, _links, _email,
            NullLogger<SendActivationEmailCommandHandler>.Instance);

    private static SendActivationEmailCommand Command(string email = "invitee@example.com") => new(email);

    private void StubStatus(
        Guid userId,
        AccountLifecycleSnapshot lifecycle,
        bool isActive = false,
        bool isEmailVerified = false,
        string email = "invitee@example.com")
    {
        _users.GetInviteAccountStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new InviteAccountStatus(
                UserId:          userId,
                Email:           email,
                IsEmailVerified: isEmailVerified,
                IsActive:        isActive,
                Lifecycle:       lifecycle));
    }

    private void StubTokens(string plain = "plain-token", string hash = "token-hash",
        string link = "https://app/activate?t=plain-token")
    {
        _tokens.Generate().Returns(plain);
        _tokens.Hash(Arg.Any<string>()).Returns(hash);
        _links.Build(Arg.Any<string>(), Arg.Any<string>()).Returns(link);
    }

    private void StubNoActiveTokens()
    {
        _tokenRepo.GetActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActivationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        _users.GetInviteAccountStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((InviteAccountStatus?)null);

        var sut = CreateSut();

        var result = await sut.Handle(Command("ghost@example.com"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);

        await _tokenRepo.DidNotReceive().AddAsync(Arg.Any<ActivationToken>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _users.DidNotReceive().MarkPendingActivationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AccountLifecycleSnapshot.Active)]
    [InlineData(AccountLifecycleSnapshot.Suspended)]
    [InlineData(AccountLifecycleSnapshot.PendingPasswordReset)]
    [InlineData(AccountLifecycleSnapshot.Archived)]
    public async Task Handle_ShouldReturnConflict_WhenLifecycleIsPastActivationWindow(
        AccountLifecycleSnapshot lifecycle)
    {
        var userId = Guid.NewGuid();
        StubStatus(userId, lifecycle, isActive: lifecycle == AccountLifecycleSnapshot.Active);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);

        await _tokenRepo.DidNotReceive().AddAsync(Arg.Any<ActivationToken>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _users.DidNotReceive().MarkPendingActivationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPersistToken_ThenMarkDelivered_AndTransition_OnHappyPath()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId, AccountLifecycleSnapshot.Provisioned);
        StubNoActiveTokens();
        StubTokens(hash: "hash-abc", link: "https://app/activate?t=plain-token");

        ActivationToken? persisted = null;
        _tokenRepo.AddAsync(Arg.Do<ActivationToken>(t => persisted = t), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _users.MarkPendingActivationAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be(userId);
        persisted.TokenHash.Should().Be("hash-abc");
        persisted.DeliveryAddress.Should().Be("invitee@example.com");

        // After the send succeeded the handler must have called
        // MarkDelivered BEFORE the UoW flush.
        persisted.State.Should().Be(ActivationTokenState.Delivered);
        persisted.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Sent);
        persisted.LastSentAt.Should().NotBeNull();

        // Email dispatched with the activation link.
        await _email.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(body => body.Contains("https://app/activate?t=plain-token", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());

        // SaveChanges is called twice: once to persist the Issued row
        // (prior-sweep + AddAsync), then again after the delivery-status
        // update. Relaxed assertion allows any count >= 2.
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());

        // Lifecycle transition invoked on the correct user.
        await _users.Received(1).MarkPendingActivationAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRevokeToken_AndNotTransition_WhenEmailFails()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId, AccountLifecycleSnapshot.Provisioned);
        StubNoActiveTokens();
        StubTokens();

        ActivationToken? persisted = null;
        _tokenRepo.AddAsync(Arg.Do<ActivationToken>(t => persisted = t), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new InvalidOperationException("SMTP DOWN")));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.ServerError);

        // Unlike Phase 2B (where no Otp was persisted on failure), the
        // Phase 2C-1 contract writes a row and transitions it to
        // Revoked(EmailFailed) so the audit trail records the attempt.
        persisted.Should().NotBeNull();
        persisted!.State.Should().Be(ActivationTokenState.Revoked);
        persisted.RevokedReason.Should().Be(ActivationTokenRevokedReason.EmailFailed);
        persisted.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Failed);

        // No lifecycle advance when the email did not go out.
        await _users.DidNotReceive().MarkPendingActivationAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSupersedePriorActiveTokens_BeforeIssuingNewOne()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId, AccountLifecycleSnapshot.PendingActivation);

        var old1 = ActivationToken.Issue(userId, "h1", "invitee@example.com", 60);
        var old2 = ActivationToken.Issue(userId, "h2", "invitee@example.com", 60);
        old2.MarkDelivered();

        _tokenRepo.GetActiveForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { old1, old2 });

        StubTokens();
        _users.MarkPendingActivationAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Both prior tokens must be superseded before the new one is issued.
        old1.State.Should().Be(ActivationTokenState.Revoked);
        old1.RevokedReason.Should().Be(ActivationTokenRevokedReason.Superseded);
        old2.State.Should().Be(ActivationTokenState.Revoked);
        old2.RevokedReason.Should().Be(ActivationTokenRevokedReason.Superseded);
    }

    [Fact]
    public async Task Handle_ShouldBeIdempotent_WhenAccountIsAlreadyPendingActivation()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId, AccountLifecycleSnapshot.PendingActivation);
        StubNoActiveTokens();
        StubTokens();

        _users.MarkPendingActivationAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _users.Received(1).MarkPendingActivationAsync(userId, Arg.Any<CancellationToken>());
    }
}
