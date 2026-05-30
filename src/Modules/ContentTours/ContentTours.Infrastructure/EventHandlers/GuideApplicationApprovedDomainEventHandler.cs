using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class GuideApplicationApprovedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<GuideApplicationApprovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<GuideApplicationApprovedDomainEvent>>
{
    public Task Handle(DomainEventNotification<GuideApplicationApprovedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new GuideApplicationApprovedIntegrationEvent(
                evt.ApplicationId,
                evt.GuideUserId,
                evt.TourId,
                evt.ApprovedByAdminId)));

        logger.LogInformation(
            "Guide application {ApplicationId} approved for tour {TourId} by admin {AdminId}",
            evt.ApplicationId, evt.TourId, evt.ApprovedByAdminId);

        return Task.CompletedTask;
    }
}
