using System.Linq.Expressions;
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
/// Phase 2C-1 — the handler now consumes <see cref="ActivationToken"/>
/// rows (primary path) and falls back to legacy <c>Otp(UserInvite)</c>
/// rows only when no <see cref="ActivationToken"/> exists for the user.
/// These tests cover:
///   • happy path on ActivationToken,
///   • bad / expired / exhausted / missing token branches,
///   • already-onboarded refusal,
///   • Otp fallback when no ActivationToken exists,
///   • session revocation invariant on success.
/// </summary>
public sealed class ActivateAccountCommandHandlerTests
{
    private const string InvitePurpose = "UserInvite";

    private readonly IUserRegistrationService   _users             = Substitute.For<IUserRegistrationService>();
    private readonly IActivationTokenRepository _tokenRepo         = Substitute.For<IActivationTokenRepository>();
    private readonly IOtpRepository             _otpRepo           = Substitute.For<IOtpRepository>();
    private readonly IAuthUnitOfWork            _uow               = Substitute.For<IAuthUnitOfWork>();
    private readonly IInviteTokenService        _tokens            = Substitute.For<IInviteTokenService>();
    private readonly ISessionRevocationService  _sessionRevocation = Substitute.For<ISessionRevocationService>();

    private ActivateAccountCommandHandler CreateSut() =>
        new(_users, _tokenRepo, _otpRepo, _uow, _tokens, _sessionRevocation);

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

    private void StubActivationToken(ActivationToken? token)
    {
        _tokenRepo.GetLatestActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(token);
    }

    private void StubOtherActivationTokens(params ActivationToken[] siblings)
    {
        _tokenRepo.GetActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(siblings);
    }

    private void StubLegacyOtp(Otp? otp)
    {
        _otpRepo.FirstOrDefaultAsync(
                Arg.Any<Expression<Func<Otp, bool>>>(),
                Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
                Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(otp);
    }

    private void StubLegacyOtpList(params Otp[] others)
    {
        _otpRepo.GetAllAsync(
                Arg.Any<Expression<Func<Otp, bool>>?>(),
                Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
                Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Otp>(others));
    }

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

    // ── ActivationToken path ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldConsumeActivationToken_AndActivate_OnHappyPath()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);

        var token = ActivationToken.Issue(userId, "good-hash", "invitee@example.com", 60);
        token.MarkDelivered();
        StubActivationToken(token);
        StubOtherActivationTokens(); // no siblings
        StubLegacyOtpList();          // no stray legacy rows

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

        // Token was consumed (not just marked used).
        token.State.Should().Be(ActivationTokenState.Consumed);
        token.ConsumedAt.Should().NotBeNull();
        token.AttemptCount.Should().Be(1);

        // Security was asked to finalize via the new verb.
        await _users.Received(1).CompleteActivationAsync(
            userId, "invitee@example.com", "Pa55word!", Arg.Any<CancellationToken>());

        // Session revocation invariant.
        await _sessionRevocation.Received(1).RevokeAllForUserAsync(
            userId, SessionRevocationReason.AccountActivated, Arg.Any<CancellationToken>());

        // Legacy-Otp fallback path was NOT entered on this request.
        await _otpRepo.DidNotReceive().FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalid_AndPersistAttempt_WhenActivationTokenHashDoesNotMatch()
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
        token.State.Should().Be(ActivationTokenState.Delivered, "a wrong-hash try must NOT consume the token");
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());

        await _users.DidNotReceive().CompleteActivationAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnTooManyRequests_WhenActivationTokenIsExhausted()
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
    public async Task Handle_ShouldReturnInvalidExpired_WhenActivationTokenExpired()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);

        // Issue a token with a 1-minute expiry, then simulate expiry by
        // waiting. Use negative expiry via subsequent reflection? Simpler:
        // construct the token normally then pass a huge nowUtc to the
        // IsExpired check. But the handler uses IsExpired() without a
        // parameter. Work around by using a 1-minute expiry and asserting
        // after the handler via explicit expiry probe? That's fragile.
        //
        // Use a tiny trick: Issue with expiry minutes = 1, then wait in
        // test via Thread.Sleep? Too slow. Instead, rely on the test-only
        // IsExpired(nowUtc) method — but the handler calls the parameterless
        // overload.
        //
        // Cleanest: expire the token via direct state manipulation through
        // the public Supersede / etc.? That changes State, not expiry.
        //
        // We'll simulate via a past-dated issue: create the token then use
        // reflection to set ExpiresAt into the past. This is acceptable
        // here because we're exercising the handler's expiry short-circuit.
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
        StubLegacyOtpList();

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
    public async Task Handle_ShouldMarkLegacyOtpsUsed_OnActivationTokenSuccess()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);

        var token = ActivationToken.Issue(userId, "good", "invitee@example.com", 60);
        token.MarkDelivered();
        StubActivationToken(token);
        StubOtherActivationTokens();

        var strayOtp = Otp.Create(userId, InvitePurpose, "h", "Email", "invitee@example.com", 60);
        StubLegacyOtpList(strayOtp);

        _tokens.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _users.CompleteActivationAsync(userId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _sessionRevocation.RevokeAllForUserAsync(
                Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>())
            .Returns(new SessionRevocationOutcome(0, 0));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        strayOtp.IsUsed.Should().BeTrue(
            "stray legacy invite rows must be consumed alongside the redeemed ActivationToken");
    }

    // ── Otp legacy fallback path ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldFallBackToLegacyOtp_WhenNoActivationTokenExists()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);
        StubActivationToken(null); // no 2C-1 token for this user

        var otp = Otp.Create(userId, InvitePurpose, "legacy-hash", "Email", "invitee@example.com", 60);
        StubLegacyOtp(otp);
        StubLegacyOtpList(); // no other legacy rows

        _tokens.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _users.CompleteActivationAsync(userId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _sessionRevocation.RevokeAllForUserAsync(
                Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>())
            .Returns(new SessionRevocationOutcome(0, 0));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        otp.IsUsed.Should().BeTrue("legacy Otp row must be marked used on success");
        otp.AttemptCount.Should().Be(1);

        await _users.Received(1).CompleteActivationAsync(
            userId, "invitee@example.com", "Pa55word!", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenNeitherActivationTokenNorLegacyOtpExists()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);
        StubActivationToken(null);
        StubLegacyOtp(null);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenLegacyOtpHashDoesNotMatch()
    {
        var userId = Guid.NewGuid();
        StubStatus(userId);
        StubActivationToken(null);

        var otp = Otp.Create(userId, InvitePurpose, "good-hash", "Email", "invitee@example.com", 60);
        StubLegacyOtp(otp);

        _tokens.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(Command("bad"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
        otp.AttemptCount.Should().Be(1);
        otp.IsUsed.Should().BeFalse();
    }
}
