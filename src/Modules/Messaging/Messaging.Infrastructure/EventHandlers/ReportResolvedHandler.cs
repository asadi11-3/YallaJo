using MediatR;
using Messaging.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Social.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Messaging.Infrastructure.EventHandlers;

/// <summary>
/// Consumes <see cref="ReportResolvedIntegrationEvent"/>. The event does not carry the
/// reporter's UserId, so direct notification of the reporter requires a Social-side lookup
/// (out of scope here). For now this handler logs the resolution as an audit trail; a
/// future enrichment step can join with Social's Report aggregate to fan out a notification.
/// </summary>
public sealed class ReportResolvedHandler(
    IMessagingInboxStore inboxStore,
    IMessagingUnitOfWork unitOfWork,
    ILogger<ReportResolvedHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ReportResolvedIntegrationEvent>>
{
    public async Task Handle(IntegrationEventNotification<ReportResolvedIntegrationEvent> notification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;
        var evt = notification.Event;

        logger.LogInformation(
            "Messaging: Report {ReportId} resolved by Admin {AdminId} on {EntityType} {EntityId} with action {Action} at {ResolvedAt:o}",
            evt.ReportId, evt.AdminUserId, evt.EntityType, evt.EntityId, evt.Action, evt.ResolvedAt);

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
