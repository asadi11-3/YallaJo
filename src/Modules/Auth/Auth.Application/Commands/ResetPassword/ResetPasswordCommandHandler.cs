using Auth.Application.Caching;
using Auth.Application.Errors;
using Auth.Application.Interfaces;
using Auth.Application.Interfaces.SessionRevocation;
using Auth.Domain.Entities;
using Auth.Domain.Errors;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    ISecurityService securityService,
    IPasswordResetTokenRepository resetTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    ITransactionalExecutor txExecutor,
    ISessionRevocationService sessionRevocation,
    HybridCache cache)
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
            return Result<ResetPasswordResult>.Fail(
                Outcome.NotFound,
                "No account found with this email.",
                AuthErrors.UserNotFound);
        }

        var token = await resetTokenRepository.GetLatestActiveForUserAsync(
            userId.Value, cancellationToken);

        if (token is null)
        {
            return Result<ResetPasswordResult>.Fail(
                Outcome.NotFound,
                "No pending reset code found. Please request a new one.",
                OtpErrors.NotFound);
        }

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
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<ResetPasswordResult>.Failure(
                Error.Validation("Otp.Invalid", "Invalid reset code."),
                Outcome.Invalid);
        }

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

        await cache.RemoveByTagAsync(
            AuthCacheKeys.UserSessionsTag(userId.Value), cancellationToken);

        return Result<ResetPasswordResult>.Success(new ResetPasswordResult(true));
    }
}
