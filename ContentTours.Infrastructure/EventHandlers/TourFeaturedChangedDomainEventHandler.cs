using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

/// <summary>
/// Handles <see cref="TourFeaturedChangedDomainEvent"/>.
/// Writes <see cref="TourFeaturedChangedIntegrationEvent"/> to the outbox.
/// Does NOT call SaveChangesAsync — UoW commits atomically.
/// </summary>
public sealed class TourFeaturedChangedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourFeaturedChangedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourFeaturedChangedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourFeaturedChangedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourFeaturedChangedIntegrationEvent(
                TourId: evt.TourId,
                IsFeatured: evt.IsFeatured,
                ChangedByUserId: evt.ChangedByUserId,
                ChangedAt: evt.ChangedAt)));

        logger.LogInformation(
            "Tour {TourId} featured flag set to {IsFeatured} by {UserId}",
            evt.TourId, evt.IsFeatured, evt.ChangedByUserId);

        return Task.CompletedTask;
    }
}
