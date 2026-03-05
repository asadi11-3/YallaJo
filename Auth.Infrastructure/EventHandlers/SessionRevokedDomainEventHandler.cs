// Auth.Infrastructure/EventHandlers/SessionRevokedDomainEventHandler.cs
using Auth.Contracts.IntegrationEvents;
using Auth.Domain.Events;
using Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Auth.Infrastructure.EventHandlers;

public sealed class SessionRevokedDomainEventHandler(
    AuthDbContext dbContext,
    ILogger<SessionRevokedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<SessionRevokedEvent>>
{
    public Task Handle(
        DomainEventNotification<SessionRevokedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling SessionRevokedEvent for user {UserId}, session {SessionId}, writing to outbox",
            domainEvent.UserId,
            domainEvent.SessionId);

        var integrationEvent = new SessionRevokedIntegrationEvent(
            domainEvent.UserId,
            domainEvent.SessionId);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
    }
}
