using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorEligibleForTierPromotionDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorEligibleForTierPromotionDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorEligibleForTierPromotionDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorEligibleForTierPromotionDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorEligibleForTierPromotionIntegrationEvent(
                CreatorProfileId: evt.CreatorProfileId,
                UserId: evt.UserId,
                CurrentTier: evt.CurrentTier.ToString(),
                EligibleForTier: evt.EligibleForTier.ToString(),
                DetectedAtUtc: DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorEligibleForTierPromotion: staged outbox for profile {ProfileId}, eligible for {EligibleForTier}.",
            evt.CreatorProfileId,
            evt.EligibleForTier);

        return Task.CompletedTask;
    }
}
