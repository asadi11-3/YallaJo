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
///   • non-existent email             → generic success (no DB write, no email).
///   • account not active / email not verified
///                                    → generic success (no DB write, no email).
///     Enterprise constraint: accounts that have never completed activation
///     (or that are currently suspended / deactivated) MUST NOT receive
///     recovery codes — recovery is a post-activation capability only.
///   • recent OTP within 60 s         → generic success (no DB write, no email).
///   • any prior active OTPs          → marked used before a new one is created.
///   • SMTP failure on send           → the OTP row is NEVER persisted, so the
///                                      domain state stays honest (previously
///                                      the row was created-then-MarkedUsed,
///                                      which lied to the audit trail by
///                                      stamping <c>UsedAt</c> on a token that
///                                      was never consumed). Enumeration
///                                      safety is preserved via the generic
///                                      success response.
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

        // Lifecycle gate: only fully-onboarded, active accounts may recover.
        // Silent rejection for any other state keeps the response
        // enumeration-safe (an observer cannot distinguish non-existent,
        // un-activated, and suspended accounts).
        var status = await securityService.GetAccountStatusByEmailAsync(normalizedEmail, ct);
        if (status is null || !status.IsActive || !status.IsEmailVerified)
        {
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
        }

        var userId = status.UserId;

        // Per-email DB throttle (defense-in-depth beyond IP/email rate limiter).
        var recentOtp = await otpRepository.FirstOrDefaultAsync(
            filter: o => o.UserId == userId
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

        // Generate the code up-front so we can send the email BEFORE we persist
        // anything. If email delivery fails, no row enters the OTP table — the
        // previous implementation created the row, failed delivery, then
        // MarkedUsed() the row, stamping UsedAt on a token that was never
        // actually consumed. That wrote a lie into the audit trail and
        // conflated three different terminal states (consumed / revoked /
        // failed-delivery) under one flag. By deferring the DB write until
        // after the email succeeds, the domain stays honest with zero schema
        // changes — the proper Revoke/Supersede/Consume distinction arrives
        // in Phase 2 with the ActivationToken / PasswordResetToken aggregates.
        var plainOtp = otpService.Generate();
        var otpHash  = otpService.Hash(plainOtp);

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
            logger.LogError(ex,
                "Auth: Failed to send PasswordReset OTP to {Email}. No OTP was persisted — the user sees a generic response for enumeration safety.",
                normalizedEmail);

            // Intentionally fall through to generic success to avoid email-existence enumeration.
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
        }

        // Email was delivered to the SMTP relay — now it is safe to persist.
        // Invalidate any prior active reset OTPs before issuing the new one
        // (in-place — the rows remain in the same unit of work).
        var oldOtps = await otpRepository.GetAllAsync(
            filter: o => o.UserId == userId
                      && o.Purpose == OtpPurpose
                      && !o.IsUsed,
            asNoTracking: false,
            ct: ct);

        foreach (var old in oldOtps)
            old.MarkUsed();

        var otp = Otp.Create(
            userId:          userId,
            purpose:         OtpPurpose,
            codeHash:        otpHash,
            deliveryChannel: "Email",
            deliveryAddress: normalizedEmail);

        await otpRepository.AddAsync(otp, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
    }
}
