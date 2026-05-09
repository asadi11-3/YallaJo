using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Events.BusinessEvents;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.EventHandlers;

/// <summary>
/// Writes a <see cref="BusinessResubmittedIntegrationEvent"/> to the outbox when a business is resubmitted.
/// Loads the business to obtain OwnerId for downstream consumers.
/// Does NOT call SaveChangesAsync — commits atomically via UoW.
/// </summary>
public sealed class BusinessResubmittedDomainEventHandler(
    IBusinessRepository businessRepository,
    ContentPlacesDbContext dbContext,
    ILogger<BusinessResubmittedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessResubmittedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BusinessResubmittedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var business = await businessRepository.GetByIdAsync(evt.BusinessId, ct);
        if (business is null)
        {
            logger.LogWarning(
                "BusinessResubmittedDomainEvent: Business {BusinessId} not found.", evt.BusinessId);
            return;
        }

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BusinessResubmittedIntegrationEvent(business.Id, business.OwnerId)));

        logger.LogInformation(
            "BusinessResubmittedDomainEvent: queued outbox for Business {BusinessId}", business.Id);
    }
}
