using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourProposalSubmittedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourProposalSubmittedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourProposalSubmittedDomainEvent>>
{
    public Task Handle(DomainEventNotification<TourProposalSubmittedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourProposalSubmittedIntegrationEvent(
                evt.ProposalId,
                evt.GuideUserId,
                evt.Title)));

        logger.LogInformation(
            "Tour proposal {ProposalId} submitted by guide {GuideUserId}: '{Title}'",
            evt.ProposalId, evt.GuideUserId, evt.Title);

        return Task.CompletedTask;
    }
}
