using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.EventHandlers;

/// <summary>
/// Reacts to a user being created in the Security module by generating a 6-digit OTP,
/// storing its hash in auth.Otps, and sending the code via email.
/// </summary>
public sealed class UserCreatedIntegrationEventHandler(
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IAuthInboxStore inboxStore,
    IOtpService otpService,
    IEmailService emailService,
    ILogger<UserCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserCreatedIntegrationEvent>>
{
    private const string EmailVerificationPurpose = "EmailVerification";
    private const string EmailDeliveryChannel = "Email";
    private const int OtpExpiryMinutes = 10;

    public async Task Handle(
        IntegrationEventNotification<UserCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        // Inbox check — idempotency guard: skip if already processed (retry/duplicate)
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Auth: Message {MessageId} (UserCreated for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var evt = notification.Event;

        // Generate cryptographically secure 6-digit OTP
        var plainOtp = otpService.Generate();
        var hashedOtp = otpService.Hash(plainOtp);

        var otp = Otp.Create(
            userId: evt.UserId,
            purpose: EmailVerificationPurpose,
            codeHash: hashedOtp,
            deliveryChannel: EmailDeliveryChannel,
            deliveryAddress: evt.Email,
            expiryMinutes: OtpExpiryMinutes);

        await otpRepository.AddAsync(otp, ct);

        // Record in inbox and persist atomically with the OTP
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        // Send OTP email (fire-and-forget relative to persistence —
        // if email fails, OTP is saved and user can request a resend)
        try
        {
            await emailService.SendAsync(
                evt.Email,
                "YallaJo — Verify Your Email",
                $"Your verification code is: {plainOtp}\n\nThis code expires in {OtpExpiryMinutes} minutes.",
                ct);

            logger.LogInformation(
                "Auth: OTP email sent to {Email} for user {UserId}.",
                evt.Email, evt.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Auth: Failed to send OTP email to {Email} for user {UserId}. OTP saved — user can request resend.",
                evt.Email, evt.UserId);
        }
    }
}
