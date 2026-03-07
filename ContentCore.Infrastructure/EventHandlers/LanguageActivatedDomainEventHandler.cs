using ContentCore.Contracts.IntegrationEvents;
using ContentCore.Domain.Events;
using ContentCore.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentCore.Infrastructure.EventHandlers;

public sealed class LanguageActivatedDomainEventHandler(
    ContentCoreDbContext dbContext,
    ILogger<LanguageActivatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<LanguageActivatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<LanguageActivatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        logger.LogInformation(
            "Handling LanguageActivatedDomainEvent for language {LanguageId} ({LanguageCode}), writing to outbox.",
            evt.LanguageId,
            evt.LanguageCode);

        var integrationEvent = new LanguageActivatedIntegrationEvent(
            evt.LanguageId,
            evt.LanguageCode);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
    }
}
