using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.EventHandlers;

/// <summary>
/// Consumes <see cref="UserCreatedIntegrationEvent"/> from the Security outbox.
/// <para>
/// Historical responsibility of this handler was to create the EmailVerification
/// OTP and send the verification email. That path has been moved INLINE into
/// <c>RegisterCommandHandler</c> so the HTTP 201 response is an honest signal
/// that the verification email was delivered. Moving it inline also eliminates
/// a race between the outbox-delayed OTP creation and any user-initiated
/// <c>ResendOtp</c> call.
/// </para>
/// <para>
/// The handler still exists to drain the outbox message idempotently and to
/// leave an audit-friendly hook in place if Auth ever needs an async side
/// effect on user creation.
/// </para>
/// </summary>
public sealed class UserCreatedIntegrationEventHandler(
    IAuthInboxStore inboxStore,
    IAuthUnitOfWork unitOfWork,
    ILogger<UserCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<UserCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            return;
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Auth: UserCreated acknowledged for user {UserId} (inbox {MessageId}). OTP/email handled inline by RegisterCommandHandler.",
            notification.Event.UserId, notification.MessageId);
    }
}
