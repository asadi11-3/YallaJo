using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

/// <summary>
/// Atomically increments the published post count on a creator profile when a post is published.
/// Uses SQL-level atomic increment to avoid concurrency conflicts.
/// </summary>
public sealed class IncrementCreatorPublishedPostCountHandler(
    ICreatorProfileRepository profileRepo,
    ILogger<IncrementCreatorPublishedPostCountHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorPostPublishedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CreatorPostPublishedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var rows = await profileRepo.AtomicIncrementPublishedPostCountAsync(evt.CreatorProfileId, ct);

        if (rows == 0)
        {
            logger.LogWarning(
                "ContentBlogs: Cannot increment published post count — CreatorProfile {ProfileId} not found for post {PostId}.",
                evt.CreatorProfileId, evt.PostId);
            return;
        }

        logger.LogDebug(
            "ContentBlogs: Atomically incremented published post count for CreatorProfile {ProfileId}.",
            evt.CreatorProfileId);
    }
}
