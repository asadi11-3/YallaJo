using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Entities;
using Security.Domain.Events;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Security.Infrastructure.EventHandlers;

public sealed class AccountLifecycleTransitionedDomainEventHandler(
    SecurityDbContext dbContext,
    ILogger<AccountLifecycleTransitionedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<AccountLifecycleTransitionedEvent>>
{
    public Task Handle(
        DomainEventNotification<AccountLifecycleTransitionedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;

       
        var integrationEvent = new UserLifecycleChangedIntegrationEvent(
            UserId: domainEvent.UserId,
            From:   ToSnapshot(domainEvent.From),
            To:     ToSnapshot(domainEvent.To));

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        logger.LogInformation(
            "Security: AccountLifecycleTransitionedEvent for user {UserId} ({From} → {To}) — outbox row queued.",
            domainEvent.UserId,
            domainEvent.From,
            domainEvent.To);

        return Task.CompletedTask;
    }

    private static AccountLifecycleSnapshot ToSnapshot(AccountLifecycleState state)
        => (AccountLifecycleSnapshot)(int)state;
}
