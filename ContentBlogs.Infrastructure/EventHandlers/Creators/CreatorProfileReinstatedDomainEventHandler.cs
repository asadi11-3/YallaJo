using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorProfileReinstatedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorProfileReinstatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorProfileReinstatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorProfileReinstatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorProfileReinstatedIntegrationEvent(
                ProfileId:         evt.ProfileId,
                UserId:            evt.UserId,
                ReinstatedByAdminId: evt.ReinstatedByAdminId,
                ReinstatedAtUtc:   DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorProfileReinstated: staged outbox for profile {ProfileId}.",
            evt.ProfileId);

        return Task.CompletedTask;
    }
}
