using MediatR;
using Tracking.Contracts.IntegrationEvents;
using Tracking.Domain.Events;
using Tracking.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Tracking.Infrastructure.EventHandlers;

public sealed class LiveTrackingSessionEndedDomainEventHandler(TrackingDbContext dbContext)
    : INotificationHandler<DomainEventNotification<LiveTrackingSessionEndedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<LiveTrackingSessionEndedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        dbContext.OutboxMessages.Add(OutboxMessage.Create(new LiveTrackingSessionEndedIntegrationEvent(
            evt.SessionId,
            evt.UserId,
            evt.EndedAt,
            evt.Reason)));

        return Task.CompletedTask;
    }
}
