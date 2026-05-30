using ContentTours.Contracts;
using ContentTours.Domain.Events;
using ContentTours.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentTours.Infrastructure.EventHandlers;

public sealed class GuideApplicationSubmittedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<GuideApplicationSubmittedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<GuideApplicationSubmittedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<GuideApplicationSubmittedDomainEvent> notification, CancellationToken ct)
    {
        var evt = notification.Event;

        var tourOwnerUserId = await dbContext.Tours
            .Where(t => t.Id == evt.TourId)
            .Select(t => t.CreatedByUserId)
            .FirstOrDefaultAsync(ct);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new NewGuideApplicationIntegrationEvent(
                evt.ApplicationId,
                evt.GuideUserId,
                tourOwnerUserId,
                evt.TourId)));

        logger.LogInformation(
            "Guide application {ApplicationId} submitted by guide {GuideUserId} for tour {TourId}",
            evt.ApplicationId, evt.GuideUserId, evt.TourId);
    }
}
