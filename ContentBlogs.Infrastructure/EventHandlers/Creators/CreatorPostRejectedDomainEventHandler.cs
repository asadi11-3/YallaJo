using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorPostRejectedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorPostRejectedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorPostRejectedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorPostRejectedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorPostRejectedIntegrationEvent(
                PostId: evt.PostId,
                CreatorProfileId: evt.CreatorProfileId,
                RejectedByAdminId: evt.RejectedByAdminId,
                Reason: evt.Reason,
                RejectedAtUtc: DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorPostRejected: staged outbox for post {PostId}.",
            evt.PostId);

        return Task.CompletedTask;
    }
}
