using MediatR;
using Microsoft.Extensions.Logging;
using ContentPlaces.Contracts.BusinessStaff;
using ContentPlaces.Domain.Events.BusinessStaffEvents;
using ContentPlaces.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.EventHandlers;

public sealed class BusinessStaffRemovedDomainEventHandler(
    ContentPlacesDbContext dbContext,
    ILogger<BusinessStaffRemovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessStaffRemovedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BusinessStaffRemovedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling BusinessStaffRemovedDomainEvent for Staff {StaffId}",
            domainEvent.StaffId);

        var integrationEvent = new BusinessStaffRemovedIntegrationEvent(
            domainEvent.StaffId,
            domainEvent.BusinessId,
            domainEvent.UserId);

        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
    }
}
