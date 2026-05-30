using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourApprovedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourApprovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourApprovedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourApprovedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourApprovedIntegrationEvent(
                TourId:           evt.TourId,
                CreatedByUserId:  evt.CreatedByUserId,
                ApprovedByUserId: evt.ApprovedByUserId,
                ApprovedAt:       evt.ApprovedAt)));

        logger.LogInformation(
            "TourApprovedDomainEvent: tour {TourId} approved by {ApprovedByUserId} at {ApprovedAt}.",
            evt.TourId, evt.ApprovedByUserId, evt.ApprovedAt);

        return Task.CompletedTask;
    }
}
