using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Events.BusinessEvents;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.EventHandlers;

public sealed class BusinessUpdatedDomainEventHandler(
    IBusinessRepository businessRepository,
    ContentPlacesDbContext dbContext,
    ILogger<BusinessUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessUpdatedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BusinessUpdatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;
        var business = await businessRepository.GetByIdAsync(evt.BusinessId, ct, asNoTracking: true);

        if (business is null)
        {
            logger.LogWarning(
                "BusinessUpdatedDomainEvent: Business {BusinessId} not found; skipping outbox.",
                evt.BusinessId);
            return;
        }

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BusinessUpdatedIntegrationEvent(
                business.Id,
                business.Name,
                business.Slug,
                business.PlaceId,
                business.IsHalal,
                business.HasVegetarianOptions,
                business.HasAlcoholFreeArea)));

        logger.LogInformation(
            "BusinessUpdatedDomainEvent: queued outbox for Business {BusinessId}",
            business.Id);
    }
}
