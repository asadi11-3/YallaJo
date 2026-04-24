using Auth.Contracts.IntegrationEvents;
using Auth.Domain.Events;
using Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Auth.Infrastructure.EventHandlers;

/// <summary>
/// Phase 2C-3 — translates the in-process
/// <see cref="ActivationTokenIssuedEvent"/> raised by the
/// <c>SendActivationEmailCommandHandler</c> into an
/// <see cref="ActivationTokenIssuedIntegrationEvent"/> persisted in the
/// Auth outbox. Runs inside the same <see cref="AuthDbContext"/> as the
/// token write so the token row + outbox message commit atomically via
/// the unit of work.
/// <para>
/// Mirrors the shape of <c>SessionRevokedDomainEventHandler</c> — no new
/// mechanism is introduced; this is a by-the-book outbox-pattern entry
/// point.
/// </para>
/// </summary>
public sealed class ActivationTokenIssuedDomainEventHandler(
    AuthDbContext dbContext,
    ILogger<ActivationTokenIssuedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<ActivationTokenIssuedEvent>>
{
    public Task Handle(
        DomainEventNotification<ActivationTokenIssuedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling ActivationTokenIssuedEvent for token {TokenId} / user {UserId}, writing to outbox.",
            domainEvent.TokenId, domainEvent.UserId);

        var integrationEvent = new ActivationTokenIssuedIntegrationEvent(
            TokenId:         domainEvent.TokenId,
            UserId:          domainEvent.UserId,
            DeliveryAddress: domainEvent.DeliveryAddress,
            PlainToken:      domainEvent.PlainToken,
            ActivationLink:  domainEvent.ActivationLink,
            ExpiresAt:       domainEvent.ExpiresAt);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
    }
}
