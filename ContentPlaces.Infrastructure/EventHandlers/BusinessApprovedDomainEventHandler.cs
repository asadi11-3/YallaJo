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
/// Writes a <see cref="BusinessApprovedIntegrationEvent"/> to the outbox when a business is approved.
/// The domain event carries ReviewedByUserId; we load the business to obtain OwnerId for downstream consumers.
/// Does NOT call SaveChangesAsync — commits atomically via UoW.
/// </summary>
public sealed class BusinessApprovedDomainEventHandler(
    IBusinessRepository businessRepository,
    ContentPlacesDbContext dbContext,
    ILogger<BusinessApprovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessApprovedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BusinessApprovedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var business = await businessRepository.GetByIdAsync(evt.BusinessId, ct);
        if (business is null)
        {
            logger.LogWarning(
                "BusinessApprovedDomainEvent: Business {BusinessId} not found.", evt.BusinessId);
            return;
        }

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BusinessApprovedIntegrationEvent(business.Id, business.OwnerId)));

        logger.LogInformation(
            "BusinessApprovedDomainEvent: queued outbox for Business {BusinessId}", business.Id);
    }
}
