using ContentCore.Contracts.IntegrationEvents;
using ContentCore.Domain.Events;
using ContentCore.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// Writes a <see cref="LanguageDeactivatedIntegrationEvent"/> to the outbox when a language
/// is deactivated, giving downstream modules the opportunity to react (e.g. stop
/// auto-translating into that language, hide per-language content).
///
/// <para>Does NOT call SaveChangesAsync — the UoW commits atomically after all handlers complete.</para>
///
/// <para><b>Intentional: no consumers registered yet.</b> The event queues in the outbox and
/// will be consumed by future module handlers without any changes here.
/// Matches the pattern used for ServiceItemCreated/Deleted events.</para>
/// </summary>
public sealed class LanguageDeactivatedDomainEventHandler(
    ContentCoreDbContext dbContext,
    ILogger<LanguageDeactivatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<LanguageDeactivatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<LanguageDeactivatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        logger.LogInformation(
            "LanguageDeactivatedDomainEvent: queueing outbox for language {LanguageId} ({LanguageCode}).",
            evt.LanguageId, evt.LanguageCode);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new LanguageDeactivatedIntegrationEvent(evt.LanguageId, evt.LanguageCode)));

        return Task.CompletedTask;
    }
}
