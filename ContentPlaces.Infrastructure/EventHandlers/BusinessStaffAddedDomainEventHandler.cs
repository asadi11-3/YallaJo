using ContentPlaces.Contracts.BusinessStaff;
using ContentPlaces.Domain.Events;
using ContentPlaces.Domain.Events.BusinessStaffEvents;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.EventHandlers;

public sealed class BusinessStaffAddedDomainEventHandler(
    ContentPlacesDbContext dbContext,
    ILogger<BusinessStaffAddedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessStaffAddedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BusinessStaffAddedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling BusinessStaffAddedDomainEvent for Staff {StaffId}",
            domainEvent.StaffId);

        var integrationEvent = new BusinessStaffAddedIntegrationEvent(
            domainEvent.StaffId,
            domainEvent.BusinessId,
            domainEvent.UserId,
            domainEvent.Role);

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
    }
}
