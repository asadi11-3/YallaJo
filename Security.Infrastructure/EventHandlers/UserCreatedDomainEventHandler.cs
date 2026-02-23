using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Events;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Security.Infrastructure.EventHandlers;

/// <summary>
/// Handles UserCreatedEvent by persisting a UserCreatedIntegrationEvent to the outbox.
/// Guarantees cross-module delivery via the OutboxProcessor retry mechanism.
/// </summary>
public sealed class UserCreatedDomainEventHandler(
    SecurityDbContext dbContext,
    ILogger<UserCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserCreatedEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserCreatedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling UserCreatedEvent for user {UserId}, persisting to outbox",
            domainEvent.UserId);

        var integrationEvent = new UserCreatedIntegrationEvent(
            domainEvent.UserId,
            domainEvent.Email);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
        await dbContext.SaveChangesAsync(ct);
    }
}
