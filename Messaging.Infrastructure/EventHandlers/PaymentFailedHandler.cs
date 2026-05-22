using Finance.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class PaymentFailedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<PaymentFailedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PaymentFailedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<PaymentFailedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;

        logger.LogWarning("PaymentFailedIntegrationEvent {PaymentId} has no UserId; skipping notification creation.", notification.Event.PaymentId);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
