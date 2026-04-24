using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ResetPassword;

/// <summary>
/// Phase 2C-5 — final cutover. The legacy
/// <c>Otp(Purpose="PasswordReset")</c> fallback path is removed;
/// self-service password reset now validates
/// <see cref="PasswordResetToken"/> rows exclusively.
/// <para>
/// Any in-flight reset code issued by the Phase 2B inline-SMTP
/// pipeline would, after this cutover, fail to redeem. Per the
/// operator's decision (no real users), that exposure is accepted.
/// </para>
/// <para>
/// Phase 1 atomic-revocation invariants are preserved verbatim:
/// <list type="number">
///   <item><description>Security: replace password (raises <c>PasswordResetEvent</c>).</description></item>
///   <item><description>Auth: revoke every active session + refresh token for the user.</description></item>
///   <item><description>Auth: <see cref="PasswordResetToken.Consume"/> the redeemed token.</description></item>
///   <item><description>Auth: <c>SaveChangesAsync</c> flushes (2) and (3) together under the ambient transactional executor so the credential mutation and the session tear-down commit as one unit.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class ResetPasswordCommandHandler(
    ISecurityService securityService,
    IPasswordResetTokenRepository resetTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    ITransactionalExecutor txExecutor,
    ISessionRevocationService sessionRevocation)
    : ICommandHandler<ResetPasswordCommand, ResetPasswordResult>
{
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

        // Load the latest non-terminal PasswordResetToken. No legacy
        // Otp fallback — absence is a hard NotFound.
        var token = await resetTokenRepository.GetLatestActiveForUserAsync(
            userId.Value, cancellationToken);

        if (token is null)
        {
            return Result<ResetPasswordResult>.Failure(
                Error.NotFound("Otp.NotFound", "No pending reset code found. Please request a new one."),
                Outcome.NotFound);
        }

        // Attempt-budget and expiry checks BEFORE the increment so a
        // bad-faith probe of an exhausted/expired token cannot leak
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
                    userId.Value, request.NewPassword, innerCt);
                if (!reset) return false;

                await sessionRevocation.RevokeAllForUserAsync(
                    userId.Value,
                    SessionRevocationReason.PasswordReplacedBySelf,
                    innerCt);

                token.Consume();

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
