using ContentBlogs.Contracts.IntegrationEvents.Creators;
using ContentBlogs.Domain.Events.Creators;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentBlogs.Infrastructure.EventHandlers.Creators;

public sealed class CreatorTierPromotedDomainEventHandler(
    ContentBlogsDbContext dbContext,
    ILogger<CreatorTierPromotedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorTierPromotedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<CreatorTierPromotedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new CreatorTierPromotedIntegrationEvent(
                CreatorProfileId: evt.CreatorProfileId,
                UserId: evt.UserId,
                PromotedByAdminId: evt.PromotedByAdminId,
                PreviousTier: evt.PreviousTier.ToString(),
                NewTier: evt.NewTier.ToString(),
                PromotedAtUtc: DateTime.UtcNow)));

        logger.LogInformation(
            "CreatorTierPromoted: staged outbox for profile {ProfileId}, {PreviousTier} → {NewTier}.",
            evt.CreatorProfileId,
            evt.PreviousTier,
            evt.NewTier);

        return Task.CompletedTask;
    }
}
