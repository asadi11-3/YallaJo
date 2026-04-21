using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ForgotPassword;

/// <summary>
/// Issues a password-reset OTP in an enumeration-safe, rate-limited and
/// SMTP-failure-safe manner:
///   • non-existent email         → generic success (no DB write, no email).
///   • recent OTP within 60 s     → generic success (no DB write, no email).
///   • any prior active OTPs      → marked used before a new one is created.
///   • SMTP failure on send       → the freshly-persisted OTP is invalidated
///                                  and the response is still a generic
///                                  success so email-existence cannot be
///                                  inferred by status code / timing.
/// </summary>
public sealed class ForgotPasswordCommandHandler(
    ISecurityService securityService,
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    IEmailService emailService,
    ILogger<ForgotPasswordCommandHandler> logger)
    : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private const string GenericMessage  = "If this email exists, a reset code was sent.";
    private const string OtpPurpose      = "PasswordReset";
    private const int    ThrottleSeconds = 60;

    public async Task<Result<ForgotPasswordResult>> Handle(
        ForgotPasswordCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var userId = await securityService.GetUserIdByEmailAsync(normalizedEmail, ct);
        if (userId is null)
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));

        // Per-email DB throttle (defense-in-depth beyond IP/email rate limiter).
        var recentOtp = await otpRepository.FirstOrDefaultAsync(
            filter: o => o.UserId == userId.Value
                      && o.Purpose == OtpPurpose
                      && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: true,
            ct: ct);

        if (recentOtp is not null
            && recentOtp.CreatedAt > DateTime.UtcNow.AddSeconds(-ThrottleSeconds))
        {
            // Enumeration-safe: do not leak "too fast" for valid emails vs silent success for invalid.
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
        }

        // Invalidate any prior active reset OTPs before issuing a new one.
        var oldOtps = await otpRepository.GetAllAsync(
            filter: o => o.UserId == userId.Value
                      && o.Purpose == OtpPurpose
                      && !o.IsUsed,
            asNoTracking: false,
            ct: ct);

        foreach (var old in oldOtps)
            old.MarkUsed();

        var plainOtp = otpService.Generate();
        var otpHash  = otpService.Hash(plainOtp);

        var otp = Otp.Create(
            userId:          userId.Value,
            purpose:         OtpPurpose,
            codeHash:        otpHash,
            deliveryChannel: "Email",
            deliveryAddress: normalizedEmail);

        await otpRepository.AddAsync(otp, ct);
        await unitOfWork.SaveChangesAsync(ct);

        try
        {
            await emailService.SendAsync(
                normalizedEmail,
                "YallaJo — Reset Your Password",
                $"Your password reset code is: {plainOtp}. It expires in 10 minutes.",
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            otp.MarkUsed();
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogError(ex,
                "Auth: Failed to send PasswordReset OTP to {Email}. OTP invalidated to avoid leaving an unsent active code.",
                normalizedEmail);
            // Intentionally fall through to generic success to avoid email-existence enumeration.
        }

        return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
    }
}
