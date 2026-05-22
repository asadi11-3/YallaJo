using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

/// <summary>
/// Decrements the follower count on a creator profile when a user unfollows them.
/// Runs synchronously within the same UoW — does NOT call SaveChangesAsync.
/// </summary>
public sealed class DecrementCreatorFollowerCountHandler(
    ICreatorProfileRepository profileRepo,
    ILogger<DecrementCreatorFollowerCountHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorUnfollowedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CreatorUnfollowedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var rows = await profileRepo.AtomicDecrementFollowerCountAsync(evt.CreatorProfileId, ct);

        if (rows == 0)
        {
            logger.LogWarning(
                "ContentBlogs: Cannot decrement follower count — CreatorProfile {ProfileId} not found.",
                evt.CreatorProfileId);
            return;
        }

        logger.LogDebug(
            "ContentBlogs: Atomically decremented follower count for CreatorProfile {ProfileId}.",
            evt.CreatorProfileId);
    }
}
