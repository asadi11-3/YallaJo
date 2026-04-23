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
/// Writes a <see cref="BusinessSuspendedIntegrationEvent"/> to the outbox when a business is suspended.
/// Loads the business to obtain OwnerId. The Reason is carried by the domain event.
/// Does NOT call SaveChangesAsync — commits atomically via UoW.
/// </summary>
public sealed class BusinessSuspendedDomainEventHandler(
    IBusinessRepository businessRepository,
    ContentPlacesDbContext dbContext,
    ILogger<BusinessSuspendedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessSuspendedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BusinessSuspendedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var business = await businessRepository.GetByIdAsync(evt.BusinessId, ct);
        if (business is null)
        {
            logger.LogWarning(
                "BusinessSuspendedDomainEvent: Business {BusinessId} not found.", evt.BusinessId);
            return;
        }

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BusinessSuspendedIntegrationEvent(business.Id, business.OwnerId, evt.Reason)));

        logger.LogInformation(
            "BusinessSuspendedDomainEvent: queued outbox for Business {BusinessId}", business.Id);
    }
}
