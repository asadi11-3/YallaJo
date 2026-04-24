using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ResendOtp;

/// <summary>
/// Resends a short-lived <see cref="Otp"/>-backed verification code.
/// <para>
/// Phase 2C-5 scope lock: this command is reserved for OTP purposes
/// that do NOT have their own dedicated aggregate. The password-reset
/// lifecycle is owned end-to-end by <c>ForgotPasswordCommand</c> +
/// <see cref="PasswordResetToken"/>; the activation lifecycle is owned
/// by <c>SendActivationEmailCommand</c> +
/// <see cref="ActivationToken"/>. Both purposes are rejected at the
/// validator. The only currently-legal purpose is
/// <c>EmailVerification</c>.
/// </para>
/// </summary>
public sealed class ResendOtpCommandHandler(
    ISecurityService securityService,
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    IEmailService emailService,
    ILogger<ResendOtpCommandHandler> logger)
    : ICommandHandler<ResendOtpCommand, ResendOtpResult>
{
    private const string GenericMessage = "If this email exists, a new code was sent.";

    public async Task<Result<ResendOtpResult>> Handle(
        ResendOtpCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var userId = await securityService.GetUserIdByEmailAsync(normalizedEmail, cancellationToken);
        if (userId is null)
        {
            return Result<ResendOtpResult>.Success(new ResendOtpResult(GenericMessage));
        }

        var recentOtp = await otpRepository.FirstOrDefaultAsync(
            filter: o => o.UserId == userId.Value
                      && o.Purpose == request.Purpose
                      && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: true,
            ct: cancellationToken);

        if (recentOtp is not null && recentOtp.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
        {
            return Result<ResendOtpResult>.Fail(
                Outcome.TooManyRequests,
                "Please wait before requesting a new code.");
        }

        var oldOtps = await otpRepository.GetAllAsync(
            filter: o => o.UserId == userId.Value
                      && o.Purpose == request.Purpose
                      && !o.IsUsed,
            asNoTracking: false,
            ct: cancellationToken);

        foreach (var old in oldOtps)
        {
            old.MarkUsed();
        }

        var plainOtp = otpService.Generate();
        var otpHash = otpService.Hash(plainOtp);

        var otp = Otp.Create(
            userId: userId.Value,
            purpose: request.Purpose,
            codeHash: otpHash,
            deliveryChannel: "Email",
            deliveryAddress: normalizedEmail);

        await otpRepository.AddAsync(otp, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Phase 2C-5: the subject is now a single constant —
        // ResendOtpCommandValidator rejects every purpose other than
        // EmailVerification before this handler runs.
        const string subject = "YallaJo — Verify Your Email";

        try
        {
            await emailService.SendAsync(
                normalizedEmail,
                subject,
                $"Your verification code is: {plainOtp}. It expires in 10 minutes.",
                cancellationToken);

            return Result<ResendOtpResult>.Success(new ResendOtpResult(GenericMessage));
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            otp.MarkUsed();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogError(ex,
                "Auth: Failed to resend {Purpose} OTP email to {Email}. OTP invalidated to avoid leaving an unsent active code.",
                request.Purpose,
                normalizedEmail);

            return Result<ResendOtpResult>.Failure(
                Error.Failure("Otp.EmailDeliveryFailed", "We couldn't send the email right now. Please try again shortly."),
                Outcome.ServerError);
        }
    }
}
