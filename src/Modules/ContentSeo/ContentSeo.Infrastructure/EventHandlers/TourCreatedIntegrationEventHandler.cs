using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Infrastructure.Persistence;
using ContentTours.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

/// <summary>
/// Creates an INACTIVE <see cref="SitemapEntry"/> and baseline <see cref="SeoMetadata"/>
/// when a Tour is created. Tours start in Pending status — not visible in sitemap until approved.
/// PDF §8: Tours priority=0.8, changefreq=weekly.
/// </summary>
public sealed class TourCreatedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<TourCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (TourCreated {TourId}) already processed; skipping.",
                notification.MessageId, notification.Event.TourId);
            return;
        }

        var evt = notification.Event;

        // ── SeoMetadata ───────────────────────────────────────────────────────
        var seoExists = await dbContext.SeoMetadata
            .AnyAsync(s => s.EntityType == SeoEntityType.Tour && s.EntityId == evt.TourId, ct);

        if (!seoExists)
        {
            dbContext.SeoMetadata.Add(SeoMetadata.Create(
                entityType:             SeoEntityType.Tour,
                entityId:               evt.TourId,
                metaTitle:              evt.Name,
                sitemapPriority:        0.8m,
                sitemapChangeFrequency: "weekly"));
        }

        // ── SitemapEntry — INACTIVE until tour is approved ────────────────────
        var sitemapExists = await dbContext.SitemapEntries
            .AnyAsync(s => s.EntityType == "Tour" && s.EntityId == evt.TourId, ct);

        if (!sitemapExists)
        {
            dbContext.SitemapEntries.Add(SitemapEntry.Create(
                url:             $"/tours/{evt.Slug}",
                entityType:      "Tour",
                entityId:        evt.TourId,
                changeFrequency: "weekly",
                priority:        0.8m,
                isActive:        false));   // Pending approval — not visible in sitemap
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ContentSeo: SEO records initialised (inactive) for Tour {TourId} (slug={Slug})",
            evt.TourId, evt.Slug);
    }
}
