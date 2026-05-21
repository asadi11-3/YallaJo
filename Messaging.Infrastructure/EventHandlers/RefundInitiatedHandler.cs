using Finance.Contracts.IntegrationEvents;
using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

public sealed class RefundInitiatedHandler(
    MessagingDbContext dbContext,
    IMessagingUnitOfWork unitOfWork,
    IMessagingInboxStore inboxStore,
    ILogger<RefundInitiatedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<RefundInitiatedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<RefundInitiatedIntegrationEvent> notification, CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;

        logger.LogWarning("RefundInitiatedIntegrationEvent {RefundPaymentId} has no UserId; skipping notification creation.", notification.Event.RefundPaymentId);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
