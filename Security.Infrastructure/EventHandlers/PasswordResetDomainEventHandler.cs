using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Events;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Security.Infrastructure.EventHandlers;

public sealed class PasswordResetDomainEventHandler(
    SecurityDbContext dbContext,
    ILogger<PasswordResetDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<PasswordResetEvent>>
{
    public Task Handle(
        DomainEventNotification<PasswordResetEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling PasswordResetEvent for user {UserId}, writing to outbox",
            domainEvent.UserId);

        var integrationEvent = new PasswordResetIntegrationEvent(domainEvent.UserId);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
    }
}
