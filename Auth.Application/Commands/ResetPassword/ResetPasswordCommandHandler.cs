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
    ITransactionalExecutor txExecutor)
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

        // Cross-module consistency: Security password change + Auth OTP burn
        // must commit as ONE retriable transactional unit, otherwise a blip
        // on Auth SaveChanges after Security already changed the password
        // would leave the OTP reusable for another reset attempt.
        //
        // The executor drives the block via AuthDbContext's retrying execution
        // strategy — required because EnableRetryOnFailure rejects user-
        // initiated transactions (TransactionScope) unless they're wrapped in
        // a strategy. The delegate may re-run on transient SQL failures; all
        // its operations (Security reset + Auth SaveChanges) are idempotent
        // on a single OTP and user.
        var executed = await txExecutor.ExecuteAsync(
            async innerCt =>
            {
                var reset = await securityService.ResetPasswordAsync(
                    userId.Value, request.NewPassword, innerCt);
                if (!reset) return false;

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
