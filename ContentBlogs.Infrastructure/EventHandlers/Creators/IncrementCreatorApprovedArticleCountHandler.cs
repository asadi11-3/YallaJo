using ContentBlogs.Domain.Events;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

/// <summary>
/// Increments the article count on a creator profile when a creator-authored blog is published.
/// Only fires when the blog has an <c>AuthoredByCreatorId</c>.
/// Runs synchronously within the same UoW — does NOT call SaveChangesAsync.
/// </summary>
public sealed class IncrementCreatorApprovedArticleCountHandler(
    ContentBlogsDbContext dbContext,
    ICreatorProfileRepository profileRepo,
    ILogger<IncrementCreatorApprovedArticleCountHandler> logger)
    : INotificationHandler<DomainEventNotification<BlogPublishedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BlogPublishedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        // Look up the blog to check if it was authored by a creator
        var creatorProfileId = await dbContext.Blogs
            .Where(b => b.Id == evt.BlogId)
            .Select(b => b.AuthoredByCreatorId)
            .FirstOrDefaultAsync(ct);

        if (creatorProfileId is null)
            return; // Not a creator-authored blog; nothing to do

        var rows = await profileRepo.AtomicIncrementArticleCountAsync(creatorProfileId.Value, ct);

        if (rows == 0)
        {
            logger.LogWarning(
                "ContentBlogs: Cannot increment article count — CreatorProfile {ProfileId} not found for blog {BlogId}.",
                creatorProfileId.Value, evt.BlogId);
            return;
        }

        logger.LogDebug(
            "ContentBlogs: Atomically incremented article count for CreatorProfile {ProfileId}.",
            creatorProfileId.Value);
    }
}
