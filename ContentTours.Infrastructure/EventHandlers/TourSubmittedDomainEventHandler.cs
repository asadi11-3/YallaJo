using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourSubmittedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourSubmittedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourSubmittedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourSubmittedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourSubmittedIntegrationEvent(
                TourId:          evt.TourId,
                CreatedByUserId: evt.CreatedByUserId,
                SubmittedAt:     evt.SubmittedAt)));

        logger.LogInformation(
            "TourSubmittedDomainEvent: tour {TourId} submitted for review at {SubmittedAt}.",
            evt.TourId, evt.SubmittedAt);

        return Task.CompletedTask;
    }
}
