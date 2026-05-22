using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorInvitationRedeemedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorInvitationRedeemedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorInvitationRedeemedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorInvitationRedeemedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorInvitationRedeemedIntegrationEvent(
                InvitationId:    evt.InvitationId,
                RedeemedByUserId: evt.RedeemedByUserId,
                RedeemedAtUtc:   DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorInvitationRedeemed: staged outbox for invitation {InvitationId}.",
            evt.InvitationId);

        return Task.CompletedTask;
    }
}
