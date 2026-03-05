
using Auth.Contracts.IntegrationEvents;
using Auth.Domain.Events;
using Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Auth.Infrastructure.EventHandlers;

public sealed class UserLoggedInDomainEventHandler(
    AuthDbContext dbContext,
    ILogger<UserLoggedInDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserLoggedInEvent>>
{
    public Task Handle(
        DomainEventNotification<UserLoggedInEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling UserLoggedInEvent for user {UserId}, session {SessionId}, writing to outbox",
            domainEvent.UserId,
            domainEvent.SessionId);

        var integrationEvent = new UserLoggedInIntegrationEvent(
            domainEvent.UserId,
            domainEvent.SessionId,
            domainEvent.DeviceId,
            domainEvent.IpAddress);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
    }
}
