using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Events;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Security.Infrastructure.EventHandlers;

public sealed class PhoneNumberUpdatedDomainEventHandler(
    SecurityDbContext dbContext,
    ILogger<PhoneNumberUpdatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<PhoneNumberUpdatedEvent>>
{
    public Task Handle(
        DomainEventNotification<PhoneNumberUpdatedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling PhoneNumberUpdatedEvent for user {UserId}, writing to outbox",
            domainEvent.UserId);

        var integrationEvent = new PhoneNumberUpdatedIntegrationEvent(
            domainEvent.UserId,
            domainEvent.PhoneNumber,
            domainEvent.IsPrimary);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
        return Task.CompletedTask;
    }
}
