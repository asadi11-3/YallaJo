using Auth.Application.Commands.ActivateAccount;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2C-5 — legacy Otp fallback is gone. Activation validates
/// <see cref="ActivationToken"/> rows exclusively. Coverage:
///   • pre-flight refusals (no user / already onboarded / no token),
///   • happy path (consume + revoke siblings + revoke sessions),
///   • bad hash / exhausted / expired branches,
///   • no <see cref="IOtpRepository"/> dependency is exercised
///     (the handler no longer takes one).
/// </summary>
public sealed class ActivateAccountCommandHandlerTests
{
    private readonly IUserRegistrationService   _users             = Substitute.For<IUserRegistrationService>();
    private readonly IActivationTokenRepository _tokenRepo         = Substitute.For<IActivationTokenRepository>();
    private readonly IAuthUnitOfWork            _uow               = Substitute.For<IAuthUnitOfWork>();
    private readonly IInviteTokenService        _tokens            = Substitute.For<IInviteTokenService>();
    private readonly ISessionRevocationService  _sessionRevocation = Substitute.For<ISessionRevocationService>();

    private ActivateAccountCommandHandler CreateSut() =>
        new(_users, _tokenRepo, _uow, _tokens, _sessionRevocation);

    private static ActivateAccountCommand Command(string token = "token-xyz") =>
        new(
            Email:           "invitee@example.com",
            Token:           token,
            Password:        "Pa55word!",
            ConfirmPassword: "Pa55word!");

    private void StubStatus(
        Guid userId,
        bool isEmailVerified = false,
        bool isActive = false,
        AccountLifecycleSnapshot lifecycle = AccountLifecycleSnapshot.PendingActivation)
    {
        _users.GetInviteAccountStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new InviteAccountStatus(
                UserId:          userId,
                Email:           "invitee@example.com",
                IsEmailVerified: isEmailVerified,
                IsActive:        isActive,
                Lifecycle:       lifecycle));
    }

    private void StubActivationToken(ActivationToken? token) =>
        _tokenRepo.GetLatestActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(token);

    private void StubOtherActivationTokens(params ActivationToken[] siblings) =>
        _tokenRepo.GetActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(siblings);

    // ── Pre-flight refusals ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAccountMissing()
    {
        _users.GetInviteAccountStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((InviteAccountStatus?)null);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);

        await _users.DidNotReceive().CompleteActivationAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenAccountAlreadyOnboarded()
    {
        StubStatus(Guid.NewGuid(), isActive: true, isEmailVerified: true,
            lifecycle: AccountLifecycleSnapshot.Active);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenNoActivationTokenExists()
    {
        // Phase 2C-5: absence is a hard NotFound. No Otp fallback.
        var userId = Guid.NewGuid();
        StubStatus(userId);
        StubActivationToken(null);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);

        await _users.DidNotReceive().CompleteActivationAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Token-state refusals ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnInvalid_AndPersistAttempt_WhenHashDoesNotMatch()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);

        var token = ActivationToken.Issue(userId, "good-hash", "invitee@example.com", 60);
        token.MarkDelivered();
        StubActivationToken(token);

        _tokens.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(Command("bad-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);

        token.AttemptCount.Should().Be(1);
        token.State.Should().Be(ActivationTokenState.Delivered,
            "a wrong-hash try must NOT consume the token");
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());

        await _users.DidNotReceive().CompleteActivationAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnTooManyRequests_WhenTokenIsExhausted()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);

        var token = ActivationToken.Issue(userId, "h", "invitee@example.com", 60);
        token.MarkDelivered();
        for (var i = 0; i < 5; i++)
            token.IncrementAttempt();

        StubActivationToken(token);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.TooManyRequests);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenTokenExpired()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);

        var token = ActivationToken.Issue(userId, "h", "invitee@example.com", 60);
        token.MarkDelivered();
        typeof(ActivationToken).GetProperty(nameof(ActivationToken.ExpiresAt))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(token, new object[] { DateTime.UtcNow.AddMinutes(-1) });

        StubActivationToken(token);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
    }

    // ── Happy path + sibling cleanup ──────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldConsumeToken_AndActivate_OnHappyPath()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);

        var token = ActivationToken.Issue(userId, "good-hash", "invitee@example.com", 60);
        token.MarkDelivered();
        StubActivationToken(token);
        StubOtherActivationTokens(); // no siblings

        _tokens.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        _users.CompleteActivationAsync(userId, "invitee@example.com", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _sessionRevocation.RevokeAllForUserAsync(
                userId, SessionRevocationReason.AccountActivated, Arg.Any<CancellationToken>())
            .Returns(new SessionRevocationOutcome(0, 0));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(userId);

        token.State.Should().Be(ActivationTokenState.Consumed);
        token.ConsumedAt.Should().NotBeNull();
        token.AttemptCount.Should().Be(1);

        await _users.Received(1).CompleteActivationAsync(
            userId, "invitee@example.com", "Pa55word!", Arg.Any<CancellationToken>());

        await _sessionRevocation.Received(1).RevokeAllForUserAsync(
            userId, SessionRevocationReason.AccountActivated, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSupersedeSiblingTokens_OnSuccessfulConsume()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);

        var main = ActivationToken.Issue(userId, "good", "invitee@example.com", 60);
        main.MarkDelivered();

        var sibling = ActivationToken.Issue(userId, "h2", "invitee@example.com", 60);

        StubActivationToken(main);
        StubOtherActivationTokens(main, sibling);

        _tokens.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _users.CompleteActivationAsync(userId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _sessionRevocation.RevokeAllForUserAsync(
                Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>())
            .Returns(new SessionRevocationOutcome(0, 0));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        main.State.Should().Be(ActivationTokenState.Consumed);
        sibling.State.Should().Be(ActivationTokenState.Revoked);
        sibling.RevokedReason.Should().Be(ActivationTokenRevokedReason.Superseded);
    }

    [Fact]
    public async Task Handle_ShouldStillPersistAttempt_WhenCompleteActivationFails()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);

        var token = ActivationToken.Issue(userId, "good-hash", "invitee@example.com", 60);
        token.MarkDelivered();
        StubActivationToken(token);

        _tokens.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        _users.CompleteActivationAsync(userId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(
                Error.Conflict("Invite.AlreadyCompleted", "already done"),
                Outcome.Conflict));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);

        token.AttemptCount.Should().Be(1);
        token.IsUsedOrConsumed().Should().BeFalse();
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());

        await _sessionRevocation.DidNotReceive().RevokeAllForUserAsync(
            Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());
    }
}

internal static class ActivationTokenTestExtensions
{
    /// <summary>
    /// Helper so the Phase 2C-5 "do not consume on Security failure" test
    /// reads fluently regardless of terminal-state semantics.
    /// </summary>
    public static bool IsUsedOrConsumed(this ActivationToken token) =>
        token.State == ActivationTokenState.Consumed;
}
