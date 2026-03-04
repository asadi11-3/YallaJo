// Security.Infrastructure/EventHandlers/PasswordChangedDomainEventHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Events;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Security.Infrastructure.EventHandlers;

public sealed class PasswordChangedDomainEventHandler(
    SecurityDbContext dbContext,
    ILogger<PasswordChangedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<PasswordChangedEvent>>
{
    public Task Handle(
        DomainEventNotification<PasswordChangedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling PasswordChangedEvent for user {UserId}, writing to outbox",
            domainEvent.UserId);

        var integrationEvent = new PasswordChangedIntegrationEvent(domainEvent.UserId);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
    }
}
