using Finance.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class PayoutScheduledHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<PayoutScheduledHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PayoutScheduledIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<PayoutScheduledIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.ProviderId, NotificationType.PayoutScheduled, NotificationChannel.InApp, "Payout Scheduled", $"Your payout of {evt.NetAmount} {evt.Currency} has been scheduled.", NotificationPriority.Medium, entityType: "Payout", entityId: evt.PayoutId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued payout scheduled notification for payout {PayoutId}", evt.PayoutId);
    }
}
