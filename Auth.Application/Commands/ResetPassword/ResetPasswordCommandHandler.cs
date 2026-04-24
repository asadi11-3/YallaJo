using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ResetPassword;

/// <summary>
/// Phase 2C-2 — self-service password reset, now backed by the dedicated
/// <see cref="PasswordResetToken"/> aggregate. Keeps a legacy
/// <c>Otp(Purpose="PasswordReset")</c> fallback so reset codes issued by
/// the Phase 2B ForgotPassword handler before the 2C-2 deploy keep
/// working for the remainder of their 10-minute window.
/// <para>
/// Phase 1 atomic-revocation invariants are preserved verbatim:
/// <list type="number">
///   <item><description>Security: replace password (self-service — raises <c>PasswordResetEvent</c>).</description></item>
///   <item><description>Auth: revoke every active session + refresh token for the user.</description></item>
///   <item><description>Auth: consume the token (<see cref="PasswordResetToken.Consume"/> on the new path, <c>Otp.MarkUsed</c> on the fallback path).</description></item>
///   <item><description>Auth: <c>SaveChangesAsync</c> flushes (2) and (3) together under the ambient transactional executor so the credential mutation and the session tear-down commit as one unit.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class ResetPasswordCommandHandler(
    ISecurityService securityService,
    IPasswordResetTokenRepository resetTokenRepository,
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    ITransactionalExecutor txExecutor,
    ISessionRevocationService sessionRevocation)
    : ICommandHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private const string LegacyOtpPurpose = "PasswordReset";

    public async Task<Result<ResetPasswordResult>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var userId = await securityService.GetUserIdByEmailAsync(normalizedEmail, cancellationToken);
        if (userId is null)
        {
            return Result<ResetPasswordResult>.Failure(
                Error.NotFound("User.NotFound", "No account found with this email."),
                Outcome.NotFound);
        }

        // Try the PasswordResetToken aggregate first.
        var token = await resetTokenRepository.GetLatestActiveForUserAsync(
            userId.Value, cancellationToken);

        if (token is not null)
        {
            return await ResetViaPasswordResetTokenAsync(
                token, userId.Value, request, cancellationToken);
        }

        // Fallback: legacy Otp(PasswordReset) row from Phase 2B.
        return await ResetViaLegacyOtpAsync(userId.Value, request, cancellationToken);
    }

    // ── PasswordResetToken path (Phase 2C-2) ──────────────────────────────────

    private async Task<Result<ResetPasswordResult>> ResetViaPasswordResetTokenAsync(
        PasswordResetToken token,
        Guid userId,
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // Attempt-budget and expiry checks BEFORE the increment so a
        // bad-faith probe of an exhausted/expired token can't leak
        // one more attempt.
        if (token.IsExhausted)
        {
            return Result<ResetPasswordResult>.Fail(
                Outcome.TooManyRequests,
                "Too many verification attempts. Please request a new code.");
        }

        if (token.IsExpired())
        {
            return Result<ResetPasswordResult>.Failure(
                Error.Validation("Otp.Expired", "Reset code has expired. Please request a new one."),
                Outcome.Invalid);
        }

        token.IncrementAttempt();

        if (!otpService.Verify(request.OtpCode, token.TokenHash))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken); // persist incremented attempt
            return Result<ResetPasswordResult>.Failure(
                Error.Validation("Otp.Invalid", "Invalid reset code."),
                Outcome.Invalid);
        }

        // Cross-module atomic unit — same ordering as Phase 1:
        //   Security reset → sessions revoked → token consumed → flush.
        // If Security returns false, the ambient TransactionScope is
        // disposed without completion and everything rolls back.
        var executed = await txExecutor.ExecuteAsync(
            async innerCt =>
            {
                var reset = await securityService.ReplacePasswordBySelfAsync(
                    userId, request.NewPassword, innerCt);
                if (!reset) return false;

                await sessionRevocation.RevokeAllForUserAsync(
                    userId,
                    SessionRevocationReason.PasswordReplacedBySelf,
                    innerCt);

                token.Consume();

                // Defensive sweep: if any legacy Otp(PasswordReset) rows
                // exist alongside, mark them used so a fallback
                // resubmission cannot reuse the same email to reset
                // twice.
                var legacy = await otpRepository.GetAllAsync(
                    filter: o => o.UserId == userId
                              && o.Purpose == LegacyOtpPurpose
                              && !o.IsUsed,
                    asNoTracking: false,
                    ct: innerCt);
                foreach (var old in legacy)
                    old.MarkUsed();

                await unitOfWork.SaveChangesAsync(innerCt);
                return true;
            },
            cancellationToken);

        if (!executed)
        {
            return Result<ResetPasswordResult>.Failure(
                Error.Failure("Reset.Failed", "Could not reset password. Please try again."),
                Outcome.ServerError);
        }

        return Result<ResetPasswordResult>.Success(new ResetPasswordResult(true));
    }

    // ── Legacy Otp(PasswordReset) fallback (to be dropped in a later phase) ──

    private async Task<Result<ResetPasswordResult>> ResetViaLegacyOtpAsync(
        Guid userId,
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var otp = await otpRepository.FirstOrDefaultAsync(
            filter:  o => o.UserId == userId
                       && o.Purpose == LegacyOtpPurpose
                       && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: false,
            ct: cancellationToken);

        if (otp is null)
        {
            return Result<ResetPasswordResult>.Failure(
                Error.NotFound("Otp.NotFound", "No pending reset code found. Please request a new one."),
                Outcome.NotFound);
        }

        if (otp.IsExhausted)
        {
            return Result<ResetPasswordResult>.Fail(
                Outcome.TooManyRequests,
                "Too many verification attempts. Please request a new code.");
        }

        if (otp.IsExpired())
        {
            return Result<ResetPasswordResult>.Failure(
                Error.Validation("Otp.Expired", "Reset code has expired. Please request a new one."),
                Outcome.Invalid);
        }

        otp.IncrementAttempt();

        if (!otpService.Verify(request.OtpCode, otp.CodeHash))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<ResetPasswordResult>.Failure(
                Error.Validation("Otp.Invalid", "Invalid reset code."),
                Outcome.Invalid);
        }

        var executed = await txExecutor.ExecuteAsync(
            async innerCt =>
            {
                var reset = await securityService.ReplacePasswordBySelfAsync(
                    userId, request.NewPassword, innerCt);
                if (!reset) return false;

                await sessionRevocation.RevokeAllForUserAsync(
                    userId,
                    SessionRevocationReason.PasswordReplacedBySelf,
                    innerCt);

                otp.MarkUsed();
                await unitOfWork.SaveChangesAsync(innerCt);
                return true;
            },
            cancellationToken);

        if (!executed)
        {
            return Result<ResetPasswordResult>.Failure(
                Error.Failure("Reset.Failed", "Could not reset password. Please try again."),
                Outcome.ServerError);
        }

        return Result<ResetPasswordResult>.Success(new ResetPasswordResult(true));
    }
}
