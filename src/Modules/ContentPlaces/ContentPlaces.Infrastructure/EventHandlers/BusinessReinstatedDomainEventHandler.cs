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
/// Writes a <see cref="BusinessReinstatedIntegrationEvent"/> to the outbox when a business is reinstated.
/// Loads the business to obtain OwnerId.
/// Does NOT call SaveChangesAsync — commits atomically via UoW.
/// </summary>
public sealed class BusinessReinstatedDomainEventHandler(
    IBusinessRepository businessRepository,
    ContentPlacesDbContext dbContext,
    ILogger<BusinessReinstatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessReinstatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BusinessReinstatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var business = await businessRepository.GetByIdAsync(evt.BusinessId, ct);
        if (business is null)
        {
            logger.LogWarning(
                "BusinessReinstatedDomainEvent: Business {BusinessId} not found.", evt.BusinessId);
            return;
        }

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BusinessReinstatedIntegrationEvent(business.Id, business.OwnerId)));

        logger.LogInformation(
            "BusinessReinstatedDomainEvent: queued outbox for Business {BusinessId}", business.Id);
    }
}
