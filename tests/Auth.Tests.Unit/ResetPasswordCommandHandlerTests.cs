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
/// Phase 2C-5 — legacy <c>Otp(PasswordReset)</c> fallback is gone.
/// ResetPassword validates <see cref="PasswordResetToken"/> rows
/// exclusively. Coverage:
///   • pre-flight refusals (no user / no token),
///   • happy path (consume + session revoke + single commit),
///   • cross-module atomicity invariant on Security failure,
///   • bad hash / exhausted / expired branches,
///   • no <see cref="IOtpRepository"/> dependency is exercised
///     (the handler no longer takes one).
/// </summary>
public sealed class ResetPasswordCommandHandlerTests
{
    private readonly ISecurityService              _security          = Substitute.For<ISecurityService>();
    private readonly IPasswordResetTokenRepository _tokenRepo         = Substitute.For<IPasswordResetTokenRepository>();
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
        new(_security, _tokenRepo, _uow, _otpService, _tx, _sessionRevocation);

    private static ResetPasswordCommand Command(string email = "user@example.com") =>
        new(email, "123456", "NewPass123", "NewPass123");

    private void StubToken(PasswordResetToken? token) =>
        _tokenRepo.GetLatestActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(token);

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
    public async Task Handle_ShouldReturnNotFound_WhenNoPasswordResetTokenExists()
    {
        // Phase 2C-5: no fallback — absence is a hard NotFound.
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        StubToken(null);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);

        await _security.DidNotReceive().ReplacePasswordBySelfAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OnSuccess_ShouldReplacePassword_RevokeSessions_ConsumeToken_AndCommitOnce()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var token = PasswordResetToken.Issue(userId, "hash", "user@example.com", 10);
        token.MarkDelivered();
        StubToken(token);

        _otpService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _security.ReplacePasswordBySelfAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        token.State.Should().Be(PasswordResetTokenState.Consumed);
        token.ConsumedAt.Should().NotBeNull();
        token.AttemptCount.Should().Be(1);

        await _security.Received(1)
            .ReplacePasswordBySelfAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Phase 1 security invariant preserved.
        await _sessionRevocation.Received(1)
            .RevokeAllForUserAsync(
                userId,
                SessionRevocationReason.PasswordReplacedBySelf,
                Arg.Any<CancellationToken>());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Cross-module atomicity ────────────────────────────────────────────────

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

        // The token.Consume was invoked inside the scope but the ambient
        // TransactionScope was never Completed, so nothing commits.
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());
    }

    // ── Token-state refusals ──────────────────────────────────────────────────

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
        typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.ExpiresAt))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(token, new object[] { DateTime.UtcNow.AddMinutes(-1) });

        StubToken(token);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
    }
}
