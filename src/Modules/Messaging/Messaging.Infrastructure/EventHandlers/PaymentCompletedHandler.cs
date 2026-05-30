using Finance.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class PaymentCompletedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<PaymentCompletedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PaymentCompletedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<PaymentCompletedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        dbContext.Notifications.Add(Notification.Create(evt.UserId, NotificationType.PaymentCompleted, NotificationChannel.InApp, "Payment Received", $"Payment of {evt.Amount} {evt.Currency} has been processed successfully.", NotificationPriority.High, entityType: "Payment", entityId: evt.PaymentId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Queued payment completed notification for payment {PaymentId}", evt.PaymentId);
    }
}
