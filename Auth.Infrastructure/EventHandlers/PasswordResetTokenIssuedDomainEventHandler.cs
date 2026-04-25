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
/// <see cref="PasswordResetTokenIssuedEvent"/> raised by the
/// <c>ForgotPasswordCommandHandler</c> into a
/// <see cref="PasswordResetTokenIssuedIntegrationEvent"/> persisted in
/// the Auth outbox. Runs inside the same <see cref="AuthDbContext"/> as
/// the token write so the token row + outbox message commit atomically
/// via the unit of work.
/// </summary>
public sealed class PasswordResetTokenIssuedDomainEventHandler(
    AuthDbContext dbContext,
    ILogger<PasswordResetTokenIssuedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<PasswordResetTokenIssuedEvent>>
{
    public Task Handle(
        DomainEventNotification<PasswordResetTokenIssuedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.Event;

        logger.LogInformation(
            "Handling PasswordResetTokenIssuedEvent for token {TokenId} / user {UserId}, writing to outbox.",
            domainEvent.TokenId, domainEvent.UserId);

        var integrationEvent = new PasswordResetTokenIssuedIntegrationEvent(
            TokenId:         domainEvent.TokenId,
            UserId:          domainEvent.UserId,
            DeliveryAddress: domainEvent.DeliveryAddress,
            PlainCode:       domainEvent.PlainCode,
            ExpiresAt:       domainEvent.ExpiresAt,
            Origin:          (PasswordResetOriginSnapshot)(int)domainEvent.Origin);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        return Task.CompletedTask;
    }
}
