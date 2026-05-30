using ContentBlogs.Contracts.IntegrationEvents;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Infrastructure.EventHandlers;

/// <summary>
/// Creates an active <see cref="SitemapEntry"/> and baseline <see cref="SeoMetadata"/>
/// when a Blog is published for the first time.
/// PDF §8: Blogs priority=0.6, changefreq=monthly.
/// </summary>
public sealed class BlogPublishedIntegrationEventHandler(
    ContentSeoDbContext dbContext,
    IContentSeoUnitOfWork unitOfWork,
    IContentSeoInboxStore inboxStore,
    ILogger<BlogPublishedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<BlogPublishedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<BlogPublishedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentSeo: Message {MessageId} (BlogPublished {BlogId}) already processed; skipping.",
                notification.MessageId, notification.Event.BlogId);
            return;
        }

        var evt = notification.Event;

        // ── SeoMetadata — create if not exists ───────────────────────────────
        var seoExists = await dbContext.SeoMetadata
            .AnyAsync(s => s.EntityType == SeoEntityType.Blog && s.EntityId == evt.BlogId, ct);

        if (!seoExists)
        {
            dbContext.SeoMetadata.Add(SeoMetadata.Create(
                entityType:             SeoEntityType.Blog,
                entityId:               evt.BlogId,
                metaTitle:              evt.Title,
                sitemapPriority:        0.6m,
                sitemapChangeFrequency: "monthly"));
        }

        // ── SitemapEntry — active on publish ─────────────────────────────────
        var sitemapEntry = await dbContext.SitemapEntries
            .FirstOrDefaultAsync(s => s.EntityType == "Blog" && s.EntityId == evt.BlogId, ct);

        if (sitemapEntry is null)
        {
            dbContext.SitemapEntries.Add(SitemapEntry.Create(
                url:             $"/blog/{evt.Slug}",
                entityType:      "Blog",
                entityId:        evt.BlogId,
                changeFrequency: "monthly",
                priority:        0.6m,
                isActive:        true,
                lastModified:    evt.PublishedAt));
        }
        else if (!sitemapEntry.IsActive)
        {
            sitemapEntry.Reactivate();
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ContentSeo: SEO records initialised for Blog {BlogId} (slug={Slug})",
            evt.BlogId, evt.Slug);
    }
}
