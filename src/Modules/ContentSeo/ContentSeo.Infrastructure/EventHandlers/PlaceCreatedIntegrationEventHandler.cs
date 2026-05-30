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
/// Creates a baseline <see cref="SeoMetadata"/> and an active <see cref="SitemapEntry"/>
/// when a new Place is published.
///
/// Business rules:
/// - Every place gets SEO records immediately (places are visible from creation).
/// - MetaTitle defaults to the place name — editors own this field after creation and
///   subsequent <c>PlaceUpdatedIntegrationEvent</c> will NOT overwrite it.
/// - SitemapEntry is created with IsActive = true and priority 0.6 (places have higher
///   priority than businesses which are moderated).
/// - Idempotency beyond inbox: if SEO records already exist, skip creation (handles
///   rare inbox/DB drift). No error; just log.
/// </summary>
public sealed class PlaceCreatedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<PlaceCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PlaceCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PlaceCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        // ── §2.6 Idempotency — check FIRST ───────────────────────────────────
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (PlaceCreated {PlaceId}) already processed; skipping.",
                notification.MessageId, notification.Event.PlaceId);
            return;
        }

        var evt = notification.Event;

        // ── SeoMetadata — one row per entity, skip if already exists ─────────
        var seoExists = await dbContext.SeoMetadata
            .AnyAsync(s => s.EntityType == SeoEntityType.Place && s.EntityId == evt.PlaceId, ct);

        if (!seoExists)
        {
            dbContext.SeoMetadata.Add(SeoMetadata.Create(
                entityType:            SeoEntityType.Place,
                entityId:              evt.PlaceId,
                metaTitle:             evt.Name,
                sitemapPriority:       0.6m,
                sitemapChangeFrequency: "weekly"));
        }
        else
        {
            logger.LogInformation(
                "ContentSeo: SeoMetadata for Place {PlaceId} already exists; skipping.",
                evt.PlaceId);
        }

        // ── SitemapEntry — active immediately for places ──────────────────────
        var sitemapExists = await dbContext.SitemapEntries
            .AnyAsync(s => s.EntityType == "Place" && s.EntityId == evt.PlaceId, ct);

        if (!sitemapExists)
        {
            dbContext.SitemapEntries.Add(SitemapEntry.Create(
                url:             $"/places/{evt.Slug}",
                entityType:      "Place",
                entityId:        evt.PlaceId,
                changeFrequency: "weekly",
                priority:        0.6m,
                isActive:        true));
        }

        // ── §2.6 Mark processed LAST, then single SaveChanges ─────────────────
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ContentSeo: SEO records initialised for Place {PlaceId} (slug={Slug})",
            evt.PlaceId, evt.Slug);
    }
}
