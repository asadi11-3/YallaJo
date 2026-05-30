using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorProfileSuspendedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorProfileSuspendedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorProfileSuspendedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorProfileSuspendedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorProfileSuspendedIntegrationEvent(
                ProfileId:        evt.ProfileId,
                UserId:           evt.UserId,
                SuspendedByAdminId: evt.SuspendedByAdminId,
                Reason:           evt.Reason,
                SuspendedAtUtc:   DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorProfileSuspended: staged outbox for profile {ProfileId}.",
            evt.ProfileId);

        return Task.CompletedTask;
    }
}
