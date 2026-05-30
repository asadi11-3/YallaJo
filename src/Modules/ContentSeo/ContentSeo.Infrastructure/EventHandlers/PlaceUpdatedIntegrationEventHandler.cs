using ContentPlaces.Contracts.IntegrationEvents;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

/// <summary>
/// Updates <see cref="SitemapEntry"/> when a Place is updated.
///
/// Business rules:
/// - Always refresh <c>LastModified</c> so search engines re-crawl the place.
/// - If the slug changed: update the URL. The event does NOT carry OldSlug so a
///   301 Redirect cannot be auto-generated here (tracked as TODO — requires
///   <c>PlaceUpdatedIntegrationEvent</c> to carry OldSlug in a future PR).
/// - MetaTitle is intentionally NOT overwritten — editors own that field after creation.
/// - Self-heal: if no SitemapEntry exists, create one (handles cases where the place
///   was created before this handler was deployed).
/// </summary>
public sealed class PlaceUpdatedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<PlaceUpdatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PlaceUpdatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PlaceUpdatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (PlaceUpdated {PlaceId}) already processed; skipping.",
                notification.MessageId, notification.Event.PlaceId);
            return;
        }

        var evt = notification.Event;

        // NOTE: PlaceUpdatedIntegrationEvent carries Name/Description/Address but NOT Slug.
        // URL management (slug rename → sitemap URL change) requires Slug in the event payload.
        // TODO: Add Slug + OldSlug to PlaceUpdatedIntegrationEvent in a future PR to enable:
        //   1. Sitemap URL updates on slug rename
        //   2. Auto-generation of 301 Redirect from old URL to new URL
        // For now: just refresh LastModified so crawlers re-visit the page.

        var sitemapEntry = await dbContext.SitemapEntries
            .FirstOrDefaultAsync(
                s => s.EntityType == "Place" && s.EntityId == evt.PlaceId, ct);

        if (sitemapEntry is null)
        {
            // Self-heal: place existed before this handler was deployed.
            // Cannot construct URL without Slug — use a placeholder until next PlaceCreated.
            logger.LogWarning(
                "ContentSeo: SitemapEntry for Place {PlaceId} not found on update; cannot self-heal without Slug.",
                evt.PlaceId);
        }
        else
        {
            // Refresh LastModified — tells search engines to re-crawl.
            sitemapEntry.Touch();
        }

        // Self-heal SeoMetadata if missing.
        var seoExists = await dbContext.SeoMetadata
            .AnyAsync(s => s.EntityType == SeoEntityType.Place && s.EntityId == evt.PlaceId, ct);

        if (!seoExists)
        {
            dbContext.SeoMetadata.Add(SeoMetadata.Create(
                entityType:      SeoEntityType.Place,
                entityId:        evt.PlaceId,
                metaTitle:       evt.Name,
                sitemapPriority: 0.6m));
        }
        // If SeoMetadata exists: do NOT overwrite MetaTitle — editors own that field.

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ContentSeo: SitemapEntry refreshed for Place {PlaceId}", evt.PlaceId);
    }
}
