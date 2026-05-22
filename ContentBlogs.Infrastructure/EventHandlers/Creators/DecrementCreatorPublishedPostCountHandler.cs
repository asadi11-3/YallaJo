using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

/// <summary>
/// Atomically decrements the published post count on a creator profile when a post is removed.
/// Uses SQL-level atomic decrement with floor at 0 to avoid concurrency conflicts.
/// </summary>
public sealed class DecrementCreatorPublishedPostCountHandler(
    ICreatorProfileRepository profileRepo,
    ILogger<DecrementCreatorPublishedPostCountHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorPostRemovedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CreatorPostRemovedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var rows = await profileRepo.AtomicDecrementPublishedPostCountAsync(evt.CreatorProfileId, ct);

        if (rows == 0)
        {
            logger.LogWarning(
                "ContentBlogs: Cannot decrement published post count — CreatorProfile {ProfileId} not found for post {PostId}.",
                evt.CreatorProfileId, evt.PostId);
            return;
        }

        logger.LogDebug(
            "ContentBlogs: Atomically decremented published post count for CreatorProfile {ProfileId}.",
            evt.CreatorProfileId);
    }
}
