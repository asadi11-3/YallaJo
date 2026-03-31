using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.EventHandlers;


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
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Auth: Message {MessageId} (UserCreated for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var evt = notification.Event;
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
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
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
