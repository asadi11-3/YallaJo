using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    ISecurityService securityService,
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    ITransactionalExecutor txExecutor,
    ISessionRevocationService sessionRevocation)
    : ICommandHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private const string OtpPurpose = "PasswordReset";

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

        var otp = await otpRepository.FirstOrDefaultAsync(
            filter:  o => o.UserId == userId.Value
                       && o.Purpose == OtpPurpose
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

        // ── Domain invariants (moved out of handler into Otp entity) ─────────
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
            await unitOfWork.SaveChangesAsync(cancellationToken); // persist incremented attempt
            return Result<ResetPasswordResult>.Failure(
                Error.Validation("Otp.Invalid", "Invalid reset code."),
                Outcome.Invalid);
        }

        // Cross-module atomic unit:
        //   1. Security: replace password (self-service — raises PasswordResetEvent)
        //   2. Auth:     revoke every active session + refresh token for this user
        //   3. Auth:     burn the OTP
        //   4. Auth:     SaveChangesAsync flushes (2) and (3) together
        //
        // If Security returns false, the ambient TransactionScope is not
        // completed and disposes rollback everything. If Security succeeds
        // but the Auth SaveChanges fails, the retrying execution strategy
        // re-runs the whole delegate (Security's ReplacePasswordBySelfAsync
        // is idempotent on a single user).
        //
        // This is the Phase 1 fix for the "password changed but session
        // still valid" vulnerability class — an attacker's cookie is
        // invalidated at the same commit point as the password rotation.
        var executed = await txExecutor.ExecuteAsync(
            async innerCt =>
            {
                var reset = await securityService.ReplacePasswordBySelfAsync(
                    userId.Value, request.NewPassword, innerCt);
                if (!reset) return false;

                // Stage tracked revocations; flush happens via the UoW below.
                await sessionRevocation.RevokeAllForUserAsync(
                    userId.Value,
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
