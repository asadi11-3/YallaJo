using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Events;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Security.Infrastructure.EventHandlers;

public sealed class EmailVerifiedDomainNotificationHandler(
    SecurityDbContext dbContext,
    ILogger<EmailVerifiedDomainNotificationHandler> logger)
    : INotificationHandler<DomainEventNotification<EmailVerifiedEvent>>
{
    public Task Handle(
        DomainEventNotification<EmailVerifiedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling EmailVerifiedEvent for user {UserId}, writing to outbox",
            domainEvent.UserId);

        var integrationEvent = new EmailVerifiedIntegrationEvent(
            domainEvent.UserId,
            domainEvent.EmailId,
            domainEvent.EmailAddress);  
        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
}

}
