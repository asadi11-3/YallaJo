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
/// Creates a baseline <see cref="SeoMetadata"/> and an INACTIVE <see cref="SitemapEntry"/>
/// when a new Business is created.
///
/// Business rules:
/// - A new business starts in <c>Pending</c> status (awaiting admin approval).
///   SEO records are created immediately for admin tooling, but the SitemapEntry
///   is set <c>IsActive = false</c> so the business does NOT appear in sitemap.xml
///   or search-engine crawls until it is approved.
/// - When the business is approved, a separate ContentSeo handler (Phase 2) will
///   call <c>SitemapEntry.Reactivate()</c>. This handler is NOT in the current PR.
/// - MetaTitle defaults to the business name; editors own this field after creation.
/// - Priority 0.5 for businesses (lower than places at 0.6).
/// </summary>
public sealed class BusinessCreatedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<BusinessCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BusinessCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<BusinessCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (BusinessCreated {BusinessId}) already processed; skipping.",
                notification.MessageId, notification.Event.BusinessId);
            return;
        }

        var evt = notification.Event;

        // ── SeoMetadata ───────────────────────────────────────────────────────
        var seoExists = await dbContext.SeoMetadata
            .AnyAsync(s => s.EntityType == SeoEntityType.Business && s.EntityId == evt.BusinessId, ct);

        if (!seoExists)
        {
            dbContext.SeoMetadata.Add(SeoMetadata.Create(
                entityType:            SeoEntityType.Business,
                entityId:              evt.BusinessId,
                metaTitle:             evt.Name,
                sitemapPriority:       0.5m,
                sitemapChangeFrequency: "weekly"));
        }

        // ── SitemapEntry — INACTIVE until business is approved ────────────────
        var sitemapExists = await dbContext.SitemapEntries
            .AnyAsync(s => s.EntityType == "Business" && s.EntityId == evt.BusinessId, ct);

        if (!sitemapExists)
        {
            dbContext.SitemapEntries.Add(SitemapEntry.Create(
                url:             $"/businesses/{evt.Slug}",
                entityType:      "Business",
                entityId:        evt.BusinessId,
                changeFrequency: "weekly",
                priority:        0.5m,
                isActive:        false));   // Pending approval — not visible in sitemap
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ContentSeo: SEO records initialised (inactive) for Business {BusinessId} (slug={Slug})",
            evt.BusinessId, evt.Slug);
    }
}
