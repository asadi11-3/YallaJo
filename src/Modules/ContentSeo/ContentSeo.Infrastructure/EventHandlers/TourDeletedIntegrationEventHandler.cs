using ContentSeo.Application.Interfaces;
using ContentSeo.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

/// <summary>
/// Deactivates the <see cref="ContentSeo.Domain.Entities.SitemapEntry"/> when a Tour is deleted.
/// SEO records are never hard-deleted (audit trail + analytics history).
/// </summary>
public sealed class TourDeletedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<TourDeletedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourDeletedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourDeletedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (TourDeleted {TourId}) already processed; skipping.",
                notification.MessageId, notification.Event.TourId);
            return;
        }

        var evt = notification.Event;

        var sitemapEntry = await dbContext.SitemapEntries
            .FirstOrDefaultAsync(s => s.EntityType == "Tour" && s.EntityId == evt.TourId, ct);

        if (sitemapEntry is null)
        {
            logger.LogWarning(
                "ContentSeo: SitemapEntry for Tour {TourId} not found on delete; nothing to deactivate.",
                evt.TourId);
        }
        else if (sitemapEntry.IsActive)
        {
            sitemapEntry.Deactivate();
            logger.LogInformation(
                "ContentSeo: SitemapEntry deactivated for deleted Tour {TourId}", evt.TourId);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
