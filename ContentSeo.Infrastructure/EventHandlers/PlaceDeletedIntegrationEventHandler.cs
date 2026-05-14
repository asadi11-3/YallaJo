using ContentPlaces.Contracts.IntegrationEvents;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

/// <summary>
/// Deactivates the <see cref="ContentSeo.Domain.Entities.SitemapEntry"/> when a Place is deleted.
///
/// Business rules:
/// - Never hard-delete SEO records (audit trail + analytics history).
/// - Flip <c>SitemapEntry.IsActive = false</c> to remove from sitemap.xml output.
/// - <c>SeoMetadata</c> is intentionally left alive (historical reference; editors may
///   reactivate the place later via admin UI).
/// - If records don't exist: log warning and return (idempotent — nothing to deactivate).
/// </summary>
public sealed class PlaceDeletedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<PlaceDeletedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PlaceDeletedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PlaceDeletedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (PlaceDeleted {PlaceId}) already processed; skipping.",
                notification.MessageId, notification.Event.PlaceId);
            return;
        }

        var evt = notification.Event;

        var sitemapEntry = await dbContext.SitemapEntries
            .FirstOrDefaultAsync(
                s => s.EntityType == "Place" && s.EntityId == evt.PlaceId, ct);

        if (sitemapEntry is null)
        {
            logger.LogWarning(
                "ContentSeo: SitemapEntry for Place {PlaceId} not found on delete; nothing to deactivate.",
                evt.PlaceId);
        }
        else if (sitemapEntry.IsActive)
        {
            sitemapEntry.Deactivate();
            logger.LogInformation(
                "ContentSeo: SitemapEntry deactivated for deleted Place {PlaceId}", evt.PlaceId);
        }
        // SeoMetadata left intact — historical reference.

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
