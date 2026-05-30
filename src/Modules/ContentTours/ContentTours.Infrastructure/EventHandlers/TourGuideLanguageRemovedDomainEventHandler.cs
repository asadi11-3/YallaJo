using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourGuideLanguageRemovedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourGuideLanguageRemovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourGuideLanguageRemovedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourGuideLanguageRemovedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourGuideLanguageRemovedIntegrationEvent(
                TourGuideId: evt.TourGuideId,
                UserId:      evt.UserId,
                LanguageId:  evt.LanguageId)));

        logger.LogInformation(
            "TourGuideLanguageRemovedDomainEvent: language {LanguageId} removed from guide profile {TourGuideId}.",
            evt.LanguageId, evt.TourGuideId);

        return Task.CompletedTask;
    }
}
