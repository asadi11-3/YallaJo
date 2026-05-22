using Finance.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Consumes <see cref="InvoiceGeneratedIntegrationEvent"/> and notifies the buyer that
/// their invoice is ready to view / download. Per F-R8, an invoice is generated for every
/// completed payment so this normally fires once per successful booking payment.
/// </summary>
public sealed class InvoiceGeneratedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<InvoiceGeneratedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<InvoiceGeneratedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<InvoiceGeneratedIntegrationEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        var body =
            $"Your invoice {evt.InvoiceNumber} for {evt.AmountTotal} {evt.Currency} is ready.";

        dbContext.Notifications.Add(Notification.Create(
            evt.UserId,
            NotificationType.PaymentCompleted,
            NotificationChannel.InApp,
            "Invoice Ready",
            body,
            NotificationPriority.Medium,
            entityType: "Invoice",
            entityId: evt.InvoiceId));

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation(
            "Queued invoice generated notification for Invoice {InvoiceId} ({InvoiceNumber}) User {UserId}",
            evt.InvoiceId, evt.InvoiceNumber, evt.UserId);
    }
}
