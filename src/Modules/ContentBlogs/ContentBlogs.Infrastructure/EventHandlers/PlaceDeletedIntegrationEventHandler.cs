using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using ContentPlaces.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers;

public sealed class PlaceDeletedIntegrationEventHandler(
    IBlogRepository blogRepository,
    IContentBlogsUnitOfWork unitOfWork,
    IContentBlogsInboxStore inboxStore,
    HybridCache cache,
    ILogger<PlaceDeletedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PlaceDeletedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PlaceDeletedIntegrationEvent> notification,
        CancellationToken cancellationToken)
    {
        // ── 1. Inbox idempotency short-circuit ───────────────────────────────
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, cancellationToken)
            .ConfigureAwait(false))
        {
            logger.LogDebug(
                "ContentBlogs: Message {MessageId} (PlaceDeleted {PlaceId}) already processed; skipping.",
                notification.MessageId, notification.Event.PlaceId);
            return;
        }

        var evt = notification.Event;

        // ── 2. Poison-message guard for empty PlaceId ────────────────────────
        // Mark processed + save so we don't redeliver a malformed message
        // forever; do not invalidate cache.
        if (evt.PlaceId == Guid.Empty)
        {
            logger.LogWarning(
                "ContentBlogs: PlaceDeletedIntegrationEvent {MessageId} carried " +
                "Guid.Empty PlaceId; dropping (marking processed without mutation).",
                notification.MessageId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        // ── 3. Load affected blogs (tracking; global !IsDeleted filter honored) ─
        var affected = await blogRepository
            .GetActiveByPlaceIdAsync(evt.PlaceId, cancellationToken)
            .ConfigureAwait(false);

        // ── 4. Empty-result fast path ────────────────────────────────────────
        // Still mark the inbox row so we don't reprocess a no-op message; but
        // do NOT invalidate cache (nothing changed).
        if (affected.Count == 0)
        {
            logger.LogDebug(
                "ContentBlogs: No active blogs reference deleted Place {PlaceId}; " +
                "marking inbox processed and returning.", evt.PlaceId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        // ── 5. Snapshot for cache invalidation BEFORE mutation finalizes ─────
        var snapshot = affected
            .Select(b => new BlogCacheSnapshot(b.Id, b.Slug, b.IsFeatured))
            .ToList();

        // ── 6. Mutate each blog via the domain method ───────────────────────
        // Blog.UnlinkFromPlace is idempotent and defensive — it early-returns
        // for soft-deleted entities or entities that already have PlaceId == null
        // (won't happen here because of the repo filter, but belt-and-braces).
        var utcNow = DateTime.UtcNow;
        foreach (var blog in affected)
        {
            blog.UnlinkFromPlace(utcNow);
        }

        // ── 7. Mark inbox processed ──────────────────────────────────────────
        inboxStore.MarkAsProcessed(notification.MessageId);

        // ── 8. Single SaveChanges ───────────────────────────────────────────
        // Atomically persists: inbox row + blog UPDATEs + outbox rows for
        // BlogUpdatedDomainEvent → BlogUpdatedIntegrationEvent.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // ── 9. Cache invalidation (success only, post-SaveChanges) ──────────
        await InvalidateCacheAsync(evt.PlaceId, snapshot, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "ContentBlogs: Unlinked {BlogCount} blog(s) from deleted Place {PlaceId} " +
            "(MessageId={MessageId}).",
            affected.Count, evt.PlaceId, notification.MessageId);
    }

    private async Task InvalidateCacheAsync(
        Guid placeId,
        IReadOnlyList<BlogCacheSnapshot> snapshot,
        CancellationToken cancellationToken)
    {
        // Always-fired (3 tags, once each)
        await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.BlogsListTag, cancellationToken)
            .ConfigureAwait(false);
        await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.BlogPlaceTag(placeId), cancellationToken)
            .ConfigureAwait(false);
        await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.SitemapRenderedTag, cancellationToken)
            .ConfigureAwait(false);

        // Per affected blog
        foreach (var s in snapshot)
        {
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogTag(s.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogSlugTag(s.Slug), cancellationToken)
                .ConfigureAwait(false);
        }

        // FeaturedBlogsTag is fired AT MOST ONCE, only if any affected blog
        // was featured.
        if (snapshot.Any(s => s.IsFeatured))
        {
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.FeaturedBlogsTag, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private readonly record struct BlogCacheSnapshot(Guid Id, string Slug, bool IsFeatured);
}
