using System.Linq.Expressions;
using Auth.Application.Commands.ResetPassword;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Tests.Unit.TestDoubles;
using FluentAssertions;
using NSubstitute;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2C-2 — covers:
///   • Cross-module consistency — the ambient TransactionScope contract
///     from Phase 1 is preserved: on Security failure, NO Auth state
///     commits; on success, password replace + session revocation +
///     token consume all commit together.
///   • Phase 1 security invariant — every self-service reset revokes
///     all active sessions + refresh tokens for the user in the same
///     unit of work as the password mutation.
///   • Phase 2C-2 primary path — the handler now consumes a
///     <see cref="PasswordResetToken"/> instead of an <c>Otp</c> row.
///   • Legacy Otp fallback — for codes issued by the Phase 2B handler
///     before the 2C-2 deploy, the old <c>Otp(PasswordReset)</c> path
///     still works.
///   • Attempt-counter persistence on bad-hash branches (rate-limit
///     safety).
/// </summary>
public sealed class ResetPasswordCommandHandlerTests
{
    private const string LegacyOtpPurpose = "PasswordReset";

    private readonly ISecurityService              _security          = Substitute.For<ISecurityService>();
    private readonly IPasswordResetTokenRepository _tokenRepo         = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IOtpRepository                _otpRepo           = Substitute.For<IOtpRepository>();
    private readonly IAuthUnitOfWork               _uow               = Substitute.For<IAuthUnitOfWork>();
    private readonly IOtpService                   _otpService        = Substitute.For<IOtpService>();
    private readonly ISessionRevocationService     _sessionRevocation = Substitute.For<ISessionRevocationService>();
    private readonly PassThroughTransactionalExecutor _tx = new();

    public ResetPasswordCommandHandlerTests()
    {
        _sessionRevocation
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>())
            .Returns(new SessionRevocationOutcome(0, 0));
    }

    private ResetPasswordCommandHandler CreateSut() =>
        new(_security, _tokenRepo, _otpRepo, _uow, _otpService, _tx, _sessionRevocation);

    private static ResetPasswordCommand Command(string email = "user@example.com") =>
        new(email, "123456", "NewPass123", "NewPass123");

    private void StubToken(PasswordResetToken? token)
    {
        _tokenRepo.GetLatestActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(token);
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

    // ── Pre-flight refusal ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldFail_WhenUserNotFound()
    {
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenNeitherTokenNorLegacyOtpExists()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        StubToken(null);
        StubLegacyOtp(null);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    // ── PasswordResetToken path ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_OnSuccess_ShouldReplacePassword_RevokeSessions_ConsumeToken_AndCommitOnce()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var token = PasswordResetToken.Issue(userId, "hash", "user@example.com", 10);
        token.MarkDelivered();
        StubToken(token);
        StubLegacyOtpList(); // no stray legacy rows

        _otpService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _security.ReplacePasswordBySelfAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Token was consumed (not just marked used).
        token.State.Should().Be(PasswordResetTokenState.Consumed);
        token.ConsumedAt.Should().NotBeNull();
        token.AttemptCount.Should().Be(1);

        await _security.Received(1)
            .ReplacePasswordBySelfAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Phase 1 security invariant: session revocation in the same unit
        // of work as the password change.
        await _sessionRevocation.Received(1)
            .RevokeAllForUserAsync(
                userId,
                SessionRevocationReason.PasswordReplacedBySelf,
                Arg.Any<CancellationToken>());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSecurityResetFailure_ShouldReturn500_AndNotSaveAuthState_AndNotRevokeSessions()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var token = PasswordResetToken.Issue(userId, "hash", "user@example.com", 10);
        token.MarkDelivered();
        StubToken(token);

        _otpService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _security.ReplacePasswordBySelfAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.ServerError);

        // Token was mutated in memory (Consume called inside the scope)
        // but SaveChangesAsync MUST NOT have been called inside the scope
        // — the ambient TransactionScope never completed so the dispose
        // rolls back any enlisted work. The handler does NOT write an
        // attempt-persistence save on the success-hash branch that
        // subsequently fails in Security.
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

        // Session revocation must only run after the password write
        // succeeds.
        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnInvalidTokenHash_ShouldNotTouchSecurity_AndShouldPersistAttemptOnly()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var token = PasswordResetToken.Issue(userId, "good-hash", "user@example.com", 10);
        token.MarkDelivered();
        StubToken(token);

        _otpService.Verify("123456", "good-hash").Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);

        await _security.DidNotReceive()
            .ReplacePasswordBySelfAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());

        // The increment-attempt SaveChanges happens OUTSIDE the transaction scope.
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        token.AttemptCount.Should().Be(1);
        token.State.Should().Be(PasswordResetTokenState.Delivered,
            "a wrong-hash try must NOT consume the token");
    }

    [Fact]
    public async Task Handle_ShouldReturnTooManyRequests_WhenTokenIsExhausted()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var token = PasswordResetToken.Issue(userId, "hash", "user@example.com", 10);
        token.MarkDelivered();
        for (var i = 0; i < 5; i++)
            token.IncrementAttempt();

        StubToken(token);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.TooManyRequests);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenTokenIsExpired()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var token = PasswordResetToken.Issue(userId, "hash", "user@example.com", 10);
        token.MarkDelivered();
        // Backdate expiry to force the expired branch.
        typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.ExpiresAt))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(token, new object[] { DateTime.UtcNow.AddMinutes(-1) });

        StubToken(token);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
    }

    [Fact]
    public async Task Handle_ShouldMarkLegacyOtpsUsed_OnTokenSuccess()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var token = PasswordResetToken.Issue(userId, "hash", "user@example.com", 10);
        token.MarkDelivered();
        StubToken(token);

        // A stray Phase 2B Otp row for the same user.
        var strayOtp = Otp.Create(userId, LegacyOtpPurpose, "h", "Email", "user@example.com", 10);
        StubLegacyOtpList(strayOtp);

        _otpService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _security.ReplacePasswordBySelfAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        strayOtp.IsUsed.Should().BeTrue(
            "stray legacy reset rows must be consumed alongside the redeemed PasswordResetToken");
    }

    // ── Legacy Otp(PasswordReset) fallback path ───────────────────────────────

    [Fact]
    public async Task Handle_ShouldFallBackToLegacyOtp_WhenNoPasswordResetTokenExists()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        StubToken(null); // no 2C-2 token for this user

        var otp = Otp.Create(userId, LegacyOtpPurpose, "hash", "Email", "user@example.com", 10);
        StubLegacyOtp(otp);

        _otpService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _security.ReplacePasswordBySelfAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        otp.IsUsed.Should().BeTrue("legacy Otp row must be marked used on success");
        otp.AttemptCount.Should().Be(1);

        // Phase 1 invariant preserved on the fallback path too.
        await _sessionRevocation.Received(1)
            .RevokeAllForUserAsync(
                userId,
                SessionRevocationReason.PasswordReplacedBySelf,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnInvalidLegacyOtpHash_ShouldPersistAttempt_AndNotTouchSecurity()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        StubToken(null);

        var otp = Otp.Create(userId, LegacyOtpPurpose, "hash", "Email", "user@example.com", 10);
        StubLegacyOtp(otp);

        _otpService.Verify("123456", "hash").Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);

        await _security.DidNotReceive()
            .ReplacePasswordBySelfAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());

        otp.AttemptCount.Should().Be(1);
        otp.IsUsed.Should().BeFalse();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
