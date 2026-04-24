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
/// Covers:
///   • Cross-module consistency — the ResetPassword command wraps the
///     Security password reset + the Auth OTP burn + the Auth session
///     revocation in a single ambient TransactionScope. When Security
///     reports failure, NO Auth state may be committed.
///   • Phase 1 security fix — on a successful self-service reset, ALL
///     active sessions and refresh tokens for the user are revoked in
///     the same unit of work as the password mutation.
///   • Phase 1 contract split — the handler calls the actor-attributed
///     <c>ReplacePasswordBySelfAsync</c> verb, not the legacy
///     (now <c>[Obsolete]</c>) <c>ResetPasswordAsync</c>.
/// </summary>
public sealed class ResetPasswordCommandHandlerTests
{
    private readonly ISecurityService           _security          = Substitute.For<ISecurityService>();
    private readonly IOtpRepository             _otpRepo           = Substitute.For<IOtpRepository>();
    private readonly IAuthUnitOfWork            _uow               = Substitute.For<IAuthUnitOfWork>();
    private readonly IOtpService                _otpService        = Substitute.For<IOtpService>();
    private readonly ISessionRevocationService  _sessionRevocation = Substitute.For<ISessionRevocationService>();
    private readonly PassThroughTransactionalExecutor _tx = new();

    public ResetPasswordCommandHandlerTests()
    {
        _sessionRevocation
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>())
            .Returns(new SessionRevocationOutcome(0, 0));
    }

    private ResetPasswordCommandHandler CreateSut() =>
        new(_security, _otpRepo, _uow, _otpService, _tx, _sessionRevocation);

    private static ResetPasswordCommand Command(string email = "user@example.com") =>
        new(email, "123456", "NewPass123", "NewPass123");

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
    public async Task Handle_OnSecurityResetFailure_ShouldReturn500_AndNotSaveAuthState_AndNotRevokeSessions()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var otp = Otp.Create(userId, "PasswordReset", "hash", "Email", "user@example.com");
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(otp);

        _otpService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        _security.ReplacePasswordBySelfAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.ServerError);

        // OTP was marked used in-memory (handler mutation before the scope),
        // but SaveChangesAsync MUST NOT have been called — the ambient tx
        // was never Completed so the dispose rolls back any enlisted work.
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());

        // Session revocation must only run after the password write succeeds —
        // otherwise we'd be tearing down sessions for a reset that never
        // actually happened.
        await _sessionRevocation.DidNotReceive()
            .RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccess_ShouldReplacePassword_RevokeSessions_BurnOtp_AndCommitOnce()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var otp = Otp.Create(userId, "PasswordReset", "hash", "Email", "user@example.com");
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(otp);

        _otpService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _security.ReplacePasswordBySelfAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        otp.IsUsed.Should().BeTrue();

        await _security.Received(1)
            .ReplacePasswordBySelfAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Phase 1 security fix: every self-service reset must revoke sessions
        // + refresh tokens in the same unit of work as the password change.
        await _sessionRevocation.Received(1)
            .RevokeAllForUserAsync(
                userId,
                SessionRevocationReason.PasswordReplacedBySelf,
                Arg.Any<CancellationToken>());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnInvalidOtp_ShouldNotTouchSecurity_ShouldNotRevokeSessions_AndShouldPersistAttemptOnly()
    {
        var userId = Guid.NewGuid();
        _security.GetUserIdByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userId);

        var otp = Otp.Create(userId, "PasswordReset", "hash", "Email", "user@example.com");
        _otpRepo.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<Otp, bool>>>(),
            Arg.Any<Func<IQueryable<Otp>, IQueryable<Otp>>?>(),
            Arg.Any<Func<IQueryable<Otp>, IOrderedQueryable<Otp>>?>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(otp);

        _otpService.Verify("123456", "hash").Returns(false);

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
        otp.AttemptCount.Should().Be(1);
    }
}
