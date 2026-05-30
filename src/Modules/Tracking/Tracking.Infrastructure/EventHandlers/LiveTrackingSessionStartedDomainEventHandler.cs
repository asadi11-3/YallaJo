using MediatR;
using Tracking.Contracts.IntegrationEvents;
using Tracking.Domain.Events;
using Tracking.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Tracking.Infrastructure.EventHandlers;

public sealed class LiveTrackingSessionStartedDomainEventHandler(TrackingDbContext dbContext)
    : INotificationHandler<DomainEventNotification<LiveTrackingSessionStartedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<LiveTrackingSessionStartedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(new LiveTrackingSessionStartedIntegrationEvent(
            evt.SessionId,
            evt.UserId,
            evt.TourBookingId,
            evt.StartedAt)));

        return Task.CompletedTask;
    }
}
