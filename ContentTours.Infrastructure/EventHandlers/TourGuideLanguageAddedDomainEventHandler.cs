using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourGuideLanguageAddedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourGuideLanguageAddedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourGuideLanguageAddedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourGuideLanguageAddedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourGuideLanguageAddedIntegrationEvent(
                TourGuideId: evt.TourGuideId,
                UserId:      evt.UserId,
                LanguageId:  evt.LanguageId,
                Proficiency: evt.Proficiency)));

        logger.LogInformation(
            "TourGuideLanguageAddedDomainEvent: language {LanguageId} added to guide profile {TourGuideId}.",
            evt.LanguageId, evt.TourGuideId);

        return Task.CompletedTask;
    }
}
