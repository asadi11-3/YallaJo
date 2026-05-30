using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

/// <summary>
/// Increments the follower count on a creator profile when a user follows them.
/// Runs synchronously within the same UoW — does NOT call SaveChangesAsync.
/// </summary>
public sealed class IncrementCreatorFollowerCountHandler(
    ICreatorProfileRepository profileRepo,
    ILogger<IncrementCreatorFollowerCountHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorFollowedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<CreatorFollowedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var rows = await profileRepo.AtomicIncrementFollowerCountAsync(evt.CreatorProfileId, ct);

        if (rows == 0)
        {
            logger.LogWarning(
                "ContentBlogs: Cannot increment follower count — CreatorProfile {ProfileId} not found.",
                evt.CreatorProfileId);
            return;
        }

        logger.LogDebug(
            "ContentBlogs: Atomically incremented follower count for CreatorProfile {ProfileId}.",
            evt.CreatorProfileId);
    }
}
