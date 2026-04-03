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
            filter: o => o.UserId == userId.Value
                      && o.Purpose == OtpPurpose
                      && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: false, ct: cancellationToken);

        if (otp is null)
        {
            return Result<ResetPasswordResult>.Failure(
               Error.NotFound("Otp.NotFound", "No pending reset code found. Please request a new one."),
               Outcome.NotFound);
        }

        if (otp.AttemptCount >= MaxOtpAttempts)
        {
            return Result<ResetPasswordResult>.Fail(
                Outcome.TooManyRequests,
                "Too many verification attempts. Please request a new code.");
        }

        if (otp.ExpiresAt < DateTime.UtcNow)
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

        var reset = await securityService.ResetPasswordAsync(userId.Value, request.NewPassword, cancellationToken);
        if (!reset)
        {
            return Result<ResetPasswordResult>.Failure(
             Error.Failure("Reset.Failed", "Could not reset password. Please try again."),
             Outcome.ServerError);
        }

        otp.MarkUsed();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ResetPasswordResult>.Success(new ResetPasswordResult(true));
    }
}
