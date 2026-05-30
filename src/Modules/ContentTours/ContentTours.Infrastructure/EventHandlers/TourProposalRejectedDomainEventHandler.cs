using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourProposalRejectedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourProposalRejectedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourProposalRejectedDomainEvent>>
{
    public Task Handle(DomainEventNotification<TourProposalRejectedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourProposalRejectedIntegrationEvent(
                evt.ProposalId,
                evt.GuideUserId,
                evt.Reason)));

        logger.LogInformation(
            "Tour proposal {ProposalId} rejected for guide {GuideUserId}",
            evt.ProposalId, evt.GuideUserId);

        return Task.CompletedTask;
    }
}
