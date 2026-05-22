using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorPostRemovedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorPostRemovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorPostRemovedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorPostRemovedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorPostRemovedIntegrationEvent(
                PostId: evt.PostId,
                CreatorProfileId: evt.CreatorProfileId,
                RemovedAtUtc: DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorPostRemoved: staged outbox for post {PostId}.",
            evt.PostId);

        return Task.CompletedTask;
    }
}
