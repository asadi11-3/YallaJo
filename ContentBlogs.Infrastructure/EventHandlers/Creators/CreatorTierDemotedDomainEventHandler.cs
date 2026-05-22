using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorTierDemotedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorTierDemotedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorTierDemotedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorTierDemotedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorTierDemotedIntegrationEvent(
                CreatorProfileId: evt.CreatorProfileId,
                UserId: evt.UserId,
                PreviousTier: evt.PreviousTier.ToString(),
                NewTier: evt.NewTier.ToString(),
                Reason: evt.Reason,
                DemotedAtUtc: DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorTierDemoted: staged outbox for profile {ProfileId}, {PreviousTier} → {NewTier}.",
            evt.CreatorProfileId,
            evt.PreviousTier,
            evt.NewTier);

        return Task.CompletedTask;
    }
}
