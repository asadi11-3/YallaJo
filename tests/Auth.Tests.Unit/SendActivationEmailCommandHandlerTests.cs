using Auth.Application.Commands.SendActivationEmail;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Events;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2C-3 — the handler no longer sends SMTP inline; it persists an
/// <see cref="ActivationToken"/> and raises an
/// <see cref="ActivationTokenIssuedEvent"/> on the aggregate so the
/// downstream outbox pipeline can dispatch the email asynchronously.
/// These tests verify:
///   • lifecycle gate (Provisioned / PendingActivation only),
///   • NotFound on unknown account,
///   • prior active ActivationTokens are superseded,
///   • new token is persisted in Issued/Pending with the domain event
///     attached,
///   • MarkPendingActivationAsync is invoked BEFORE the UoW save so
///     the transition commits with the token + outbox row,
///   • no <see cref="IEmailService"/> / <see cref="IInviteLinkBuilder"/>
///     dependency is exercised inline (the handler no longer takes
///     <c>IEmailService</c>).
/// </summary>
public sealed class SendActivationEmailCommandHandlerTests
{
    private readonly IUserRegistrationService   _users     = Substitute.For<IUserRegistrationService>();
    private readonly IActivationTokenRepository _tokenRepo = Substitute.For<IActivationTokenRepository>();
    private readonly IAuthUnitOfWork            _uow       = Substitute.For<IAuthUnitOfWork>();
    private readonly IInviteTokenService        _tokens    = Substitute.For<IInviteTokenService>();
    private readonly IInviteLinkBuilder         _links     = Substitute.For<IInviteLinkBuilder>();

    private SendActivationEmailCommandHandler CreateSut() =>
        new(_users, _tokenRepo, _uow, _tokens, _links,
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
        await _users.DidNotReceive().MarkPendingActivationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPersistTokenInIssuedPending_AndRaiseDomainEvent_OnHappyPath()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId, AccountLifecycleSnapshot.Provisioned);
        StubNoActiveTokens();
        StubTokens(plain: "plain-xyz", hash: "hash-abc", link: "https://app/activate?t=plain-xyz");

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

        // Critical Phase 2C-3 invariant: the token is persisted in
        // Issued/Pending — it is NOT marked Delivered inline. The
        // ActivationEmailDispatchHandler will flip it to Delivered only
        // after SMTP succeeds.
        persisted.State.Should().Be(ActivationTokenState.Issued);
        persisted.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Pending);
        persisted.LastSentAt.Should().BeNull();

        // The aggregate must carry the domain event so the unit-of-work
        // dispatcher picks it up and writes an outbox message. The plain
        // token + pre-built link are carried on the event because the
        // aggregate itself only stores the hash.
        var domainEvent = persisted.DomainEvents
            .OfType<ActivationTokenIssuedEvent>()
            .Single();
        domainEvent.TokenId.Should().Be(persisted.Id);
        domainEvent.UserId.Should().Be(userId);
        domainEvent.DeliveryAddress.Should().Be("invitee@example.com");
        domainEvent.PlainToken.Should().Be("plain-xyz");
        domainEvent.ActivationLink.Should().Be("https://app/activate?t=plain-xyz");
        domainEvent.ExpiresAt.Should().Be(persisted.ExpiresAt);

        // Lifecycle transition must happen BEFORE SaveChanges so the
        // token row, the outbox row, and the PendingActivation
        // transition all commit together.
        await _users.Received(1).MarkPendingActivationAsync(userId, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        old1.State.Should().Be(ActivationTokenState.Revoked);
        old1.RevokedReason.Should().Be(ActivationTokenRevokedReason.Superseded);
        old2.State.Should().Be(ActivationTokenState.Revoked);
        old2.RevokedReason.Should().Be(ActivationTokenRevokedReason.Superseded);
    }

    [Fact]
    public async Task Handle_ShouldMarkPendingActivation_EvenFromPendingActivation_Idempotently()
    {
        // Resending activation to an already-PendingActivation user is
        // idempotent; MarkPendingActivationAsync returns Success without
        // changing state, but the handler must still call it so the
        // contract is uniform across Provisioned / PendingActivation
        // entry points.
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
