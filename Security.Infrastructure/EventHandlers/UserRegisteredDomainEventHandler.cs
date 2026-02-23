using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Events;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Security.Infrastructure.EventHandlers;

/// <summary>
/// Handles UserRegisteredEvent by persisting a UserRegisteredIntegrationEvent to the outbox.
/// Guarantees cross-module delivery via the OutboxProcessor retry mechanism.
/// </summary>
public sealed class UserRegisteredDomainEventHandler(
    SecurityDbContext dbContext,
    ILogger<UserRegisteredDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserRegisteredEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserRegisteredEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling UserRegisteredEvent for user {UserId}, persisting to outbox",
            domainEvent.UserId);

        var integrationEvent = new UserRegisteredIntegrationEvent(
            domainEvent.UserId,
            domainEvent.Email);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
        await dbContext.SaveChangesAsync(ct);
    }
}
