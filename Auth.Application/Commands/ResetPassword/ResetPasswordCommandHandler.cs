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
    IOtpService otpService)
    : ICommandHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private const int MaxOtpAttempts = 5;
    private const string OtpPurpose = "PasswordReset";

    public async Task<Result<ResetPasswordResult>> Handle(
        ResetPasswordCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var userId = await securityService.GetUserIdByEmailAsync(normalizedEmail, ct);
        if (userId is null)
            return Result<ResetPasswordResult>.Failure(
                Error.NotFound("User.NotFound", "No account found with this email."),
                Outcome.NotFound);

        // Get latest active OTP for password reset
        var otp = await otpRepository.FirstOrDefaultAsync(
            filter: o => o.UserId == userId.Value
                      && o.Purpose == OtpPurpose
                      && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: false,
            ct: ct);

        if (otp is null)
            return Result<ResetPasswordResult>.Failure(
                Error.NotFound("Otp.NotFound", "No pending reset code found. Please request a new one."),
                Outcome.NotFound);

        // Brute force protection
        if (otp.AttemptCount >= MaxOtpAttempts)
            return Result<ResetPasswordResult>.Fail(
                Outcome.TooManyRequests,
                "Too many verification attempts. Please request a new code.");

        // Check expiration
        if (otp.ExpiresAt < DateTime.UtcNow)
            return Result<ResetPasswordResult>.Failure(
                Error.Validation("Otp.Expired", "Reset code has expired. Please request a new one."),
                Outcome.Invalid);

        // Record attempt and verify
        otp.IncrementAttempt();

        if (!otpService.Verify(request.OtpCode, otp.CodeHash))
        {
            await unitOfWork.SaveChangesAsync(ct); // persist attempt count
            return Result<ResetPasswordResult>.Failure(
                Error.Validation("Otp.Invalid", "Invalid reset code."),
                Outcome.Invalid);
        }

        // OTP valid — mark as used
        otp.MarkUsed();
        await unitOfWork.SaveChangesAsync(ct);

        // Reset password via Security module (also raises PasswordResetDomainEvent → outbox → integration event)
        var reset = await securityService.ResetPasswordAsync(userId.Value, request.NewPassword, ct);
        if (!reset)
            return Result<ResetPasswordResult>.Failure(
                Error.Failure("Reset.Failed", "Could not reset password. Please try again."),
                Outcome.ServerError);

        return Result<ResetPasswordResult>.Success(new ResetPasswordResult(true));
    }
}
