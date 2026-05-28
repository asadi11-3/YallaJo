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
/// Writes a <see cref="BusinessMoreDocsRequestedIntegrationEvent"/> to the outbox
/// when additional documents are requested for a business.
/// Does NOT call SaveChangesAsync — commits atomically via UoW.
/// </summary>
public sealed class BusinessMoreDocsRequestedDomainEventHandler(
    IBusinessRepository businessRepository,
    ContentPlacesDbContext dbContext,
    ILogger<BusinessMoreDocsRequestedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessMoreDocsRequestedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BusinessMoreDocsRequestedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        var business = await businessRepository.GetByIdAsync(evt.BusinessId, ct);
        if (business is null)
        {
            logger.LogWarning(
                "BusinessMoreDocsRequestedDomainEvent: Business {BusinessId} not found.", evt.BusinessId);
            return;
        }

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BusinessMoreDocsRequestedIntegrationEvent(business.Id, business.OwnerId, evt.Reason)));

        logger.LogInformation(
            "BusinessMoreDocsRequestedDomainEvent: queued outbox for Business {BusinessId}", business.Id);
    }
}
