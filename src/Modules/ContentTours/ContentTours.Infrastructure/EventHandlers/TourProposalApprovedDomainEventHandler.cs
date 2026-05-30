using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class TourProposalApprovedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourProposalApprovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourProposalApprovedDomainEvent>>
{
    public Task Handle(DomainEventNotification<TourProposalApprovedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourProposalApprovedIntegrationEvent(
                evt.ProposalId,
                evt.GuideUserId,
                evt.CreatedTourId,
                evt.ApprovedByAdminId)));

        logger.LogInformation(
            "Tour proposal {ProposalId} approved, created tour {TourId} by admin {AdminId}",
            evt.ProposalId, evt.CreatedTourId, evt.ApprovedByAdminId);

        return Task.CompletedTask;
    }
}
