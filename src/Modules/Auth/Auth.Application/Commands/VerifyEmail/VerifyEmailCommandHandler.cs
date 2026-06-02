using Auth.Application.Errors;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Errors;
using Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using RefreshTokenEntity = Auth.Domain.Entities.RefreshToken;

namespace Auth.Application.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    ISecurityService securityService,
    IOtpRepository otpRepository,
    IDeviceRepository deviceRepository,
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    ITokenService tokenService,
    IRequestContext requestContext,
    ITransactionalExecutor txExecutor)
    : ICommandHandler<VerifyEmailCommand, VerifyEmailResult>
{
    private const int SessionDays      = 30;
    private const int RefreshTokenDays = 30;

    public async Task<Result<VerifyEmailResult>> Handle(
        VerifyEmailCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var userId = await securityService.GetUserIdByEmailAsync(normalizedEmail, cancellationToken);
        if (userId is null)
        {
            return Result<VerifyEmailResult>.Fail(
                Outcome.NotFound,
                "No account found with this email.",
                AuthErrors.UserNotFound);
        }

        var otp = await otpRepository.FirstOrDefaultAsync(
            filter:  o => o.UserId == userId.Value
                       && o.Purpose == "EmailVerification"
                       && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: false,
            ct: cancellationToken);

        if (otp is null)
        {
            return Result<VerifyEmailResult>.Fail(
                Outcome.NotFound,
                "No pending verification code found. Please register again.",
                OtpErrors.NotFound);
        }

        if (otp.IsExhausted)
        {
            return Result<VerifyEmailResult>.Fail(
                Outcome.TooManyRequests,
                "Too many verification attempts. Please request a new code.");
        }

        if (otp.IsExpired())
        {
            return Result<VerifyEmailResult>.Failure(
                Error.Validation("Otp.Expired", "Verification code has expired. Please request a new one."),
                Outcome.Invalid);
        }

        otp.IncrementAttempt();

        if (!otpService.Verify(request.OtpCode, otp.CodeHash))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken); // persist incremented attempt
            return Result<VerifyEmailResult>.Failure(
                Error.Validation("Otp.Invalid", "Invalid verification code."),
                Outcome.Invalid);
        }

        otp.MarkUsed();

        VerificationOutcome outcome;
        try
        {
            outcome = await txExecutor.ExecuteAsync<VerificationOutcome>(
                async innerCt =>
                {
                    var device = Device.Create(
                        userId:      userId.Value,
                        deviceToken: Guid.CreateVersion7().ToString(),
                        userAgent:   requestContext.UserAgent,
                        deviceName:  requestContext.DeviceName);
                    await deviceRepository.AddAsync(device, innerCt);

                    var session = Session.Create(
                        userId:    userId.Value,
                        deviceId:  device.Id,
                        expiresAt: DateTime.UtcNow.AddDays(SessionDays),
                        ipAddress: requestContext.IpAddress);
                    await sessionRepository.AddAsync(session, innerCt);

                    var plainRefreshToken     = tokenService.GenerateRefreshToken();
                    var refreshTokenHash      = tokenService.HashRefreshToken(plainRefreshToken);
                    var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays);

                    var refreshToken = RefreshTokenEntity.Create(
                        userId:    userId.Value,
                        sessionId: session.Id,
                        tokenHash: refreshTokenHash,
                        expiresAt: refreshTokenExpiresAt);
                    await refreshTokenRepository.AddAsync(refreshToken, innerCt);

                    await unitOfWork.SaveChangesAsync(innerCt);

                    var verified = await securityService.MarkEmailVerifiedAsync(
                        userId.Value, normalizedEmail, innerCt);

                    return new VerificationOutcome(
                        Verified:              verified,
                        SessionId:             session.Id,
                        PlainRefreshToken:     plainRefreshToken,
                        RefreshTokenExpiresAt: refreshTokenExpiresAt);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<VerifyEmailResult>.Failure(
                new Error("Otp.ConcurrencyConflict", "Verification state was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        if (!outcome.Verified)
        {
            return Result<VerifyEmailResult>.Failure(
                Error.Failure("Verification.Failed", "Could not verify email. Please try again."),
                Outcome.ServerError);
        }

        var userData    = await securityService.GetUserDataByIdAsync(userId.Value, cancellationToken);
        var accessToken = tokenService.GenerateAccessToken(new TokenData(
            UserId:           userId.Value,
            Email:            normalizedEmail,
            Roles:            userData?.Roles ?? [],
            AdditionalClaims: userData?.Claims ?? [],
            SessionId:        outcome.SessionId));

        return Result<VerifyEmailResult>.Success(new VerifyEmailResult(
            UserId:                userId.Value,
            AccessToken:           accessToken,
            RefreshToken:          outcome.PlainRefreshToken,
            RefreshTokenExpiresAt: outcome.RefreshTokenExpiresAt));
    }

    private readonly record struct VerificationOutcome(
        bool     Verified,
        Guid     SessionId,
        string   PlainRefreshToken,
        DateTime RefreshTokenExpiresAt);
}
