using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class GuideApplicationRejectedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<GuideApplicationRejectedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<GuideApplicationRejectedDomainEvent>>
{
    public Task Handle(DomainEventNotification<GuideApplicationRejectedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new GuideApplicationRejectedIntegrationEvent(
                evt.ApplicationId,
                evt.GuideUserId,
                evt.TourId,
                evt.Reason)));

        logger.LogInformation(
            "Guide application {ApplicationId} rejected for guide {GuideUserId}",
            evt.ApplicationId, evt.GuideUserId);

        return Task.CompletedTask;
    }
}
