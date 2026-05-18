using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using ContentTours.Contracts;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class TourDeletedIntegrationEventHandler(
    IBlogRepository blogRepository,
    IContentBlogsUnitOfWork unitOfWork,
    IContentBlogsInboxStore inboxStore,
    HybridCache cache,
    ILogger<TourDeletedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourDeletedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourDeletedIntegrationEvent> notification,
        CancellationToken cancellationToken)
    {
        // ── 1. Inbox idempotency short-circuit ───────────────────────────────
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, cancellationToken)
            .ConfigureAwait(false))
        {
            logger.LogDebug(
                "ContentBlogs: Message {MessageId} (TourDeleted {TourId}) already processed; skipping.",
                notification.MessageId, notification.Event.TourId);
            return;
        }

        var evt = notification.Event;

        // ── 2. Poison-message guard for empty TourId ─────────────────────────
        if (evt.TourId == Guid.Empty)
        {
            logger.LogWarning(
                "ContentBlogs: TourDeletedIntegrationEvent {MessageId} carried " +
                "Guid.Empty TourId; dropping (marking processed without mutation).",
                notification.MessageId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        // ── 3. Load affected blogs (tracking; global !IsDeleted filter honored) ─
        var affected = await blogRepository
            .GetActiveByTourIdAsync(evt.TourId, cancellationToken)
            .ConfigureAwait(false);

        // ── 4. Empty-result fast path ────────────────────────────────────────
        if (affected.Count == 0)
        {
            logger.LogDebug(
                "ContentBlogs: No active blogs reference deleted Tour {TourId}; " +
                "marking inbox processed and returning.", evt.TourId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        // ── 5. Snapshot for cache invalidation BEFORE mutation finalizes ─────
        var snapshot = affected
            .Select(b => new BlogCacheSnapshot(b.Id, b.Slug, b.IsFeatured))
            .ToList();

        // ── 6. Mutate each blog via the domain method ────────────────────────
        var utcNow = DateTime.UtcNow;
        foreach (var blog in affected)
        {
            blog.RegisterTourUnlinked(evt.TourId, utcNow);
        }

        // ── 7. Mark inbox processed ──────────────────────────────────────────
        inboxStore.MarkAsProcessed(notification.MessageId);

        // ── 8. Single SaveChanges ────────────────────────────────────────────
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // ── 9. Cache invalidation (success only, post-SaveChanges) ──────────
        await InvalidateCacheAsync(evt.TourId, snapshot, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "ContentBlogs: Unlinked {BlogCount} blog(s) from deleted Tour {TourId} " +
            "(MessageId={MessageId}).",
            affected.Count, evt.TourId, notification.MessageId);
    }

    private async Task InvalidateCacheAsync(
        Guid tourId,
        IReadOnlyList<BlogCacheSnapshot> snapshot,
        CancellationToken cancellationToken)
    {
        await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.BlogsListTag, cancellationToken)
            .ConfigureAwait(false);
        await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.SitemapRenderedTag, cancellationToken)
            .ConfigureAwait(false);

        foreach (var s in snapshot)
        {
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogTag(s.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogSlugTag(s.Slug), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogToursTag(s.Id), cancellationToken)
                .ConfigureAwait(false);
        }

        if (snapshot.Any(s => s.IsFeatured))
        {
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.FeaturedBlogsTag, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private readonly record struct BlogCacheSnapshot(Guid Id, string Slug, bool IsFeatured);
}
