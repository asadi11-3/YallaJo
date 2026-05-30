using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorFollowedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorFollowedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorFollowedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorFollowedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorFollowAddedIntegrationEvent(
                CreatorProfileId: evt.CreatorProfileId,
                FollowerUserId:   evt.FollowerUserId,
                FollowedAtUtc:    DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorFollowed: staged outbox for creator {CreatorProfileId}, follower {FollowerUserId}.",
            evt.CreatorProfileId, evt.FollowerUserId);

        return Task.CompletedTask;
    }
}
