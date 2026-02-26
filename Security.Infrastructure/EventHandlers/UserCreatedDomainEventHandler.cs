using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Events;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Security.Infrastructure.EventHandlers;


public sealed class UserCreatedDomainEventHandler(
    SecurityDbContext dbContext,
    ILogger<UserCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserCreatedEvent>>
{
    public Task Handle(
        DomainEventNotification<UserCreatedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling UserCreatedEvent for user {UserId}, writing to outbox",
            domainEvent.UserId);

        var integrationEvent = new UserCreatedIntegrationEvent(
            domainEvent.UserId,
            domainEvent.Email,
            domainEvent.FirstName,
            domainEvent.LastName);

     
        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
}

}