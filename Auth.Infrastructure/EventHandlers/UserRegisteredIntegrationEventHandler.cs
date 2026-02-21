using Accounts.Contracts.IntegrationEvents;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Infrastructure.EventHandlers;

/// <summary>
/// Reacts to a user registering in the Accounts module by creating a stub
/// UserCredentials record in the Auth store.
///
/// Triggered by OutboxProcessor when it reads a UserRegisteredIntegrationEvent
/// from the Accounts outbox and publishes it via MediatR.
///
/// Note: Auth.Infrastructure references Accounts.Application for the event type.
/// In a larger system this contract would live in a dedicated Accounts.Contracts project.
/// </summary>
public sealed class UserRegisteredIntegrationEventHandler(
    AuthDbContext dbContext,
    ILogger<UserRegisteredIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserRegisteredIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<UserRegisteredIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        logger.LogInformation(
            "Auth ← Accounts: UserRegistered received for {UserId} ({Email}). Creating credentials stub.",
            evt.UserId, evt.Email);

        // Idempotency guard — the outbox processor may retry on failure
        var existing = await dbContext.UserCredentials.FindAsync([evt.UserId], ct);
        if (existing is not null)
        {
            logger.LogWarning(
                "Auth: Credentials already exist for user {UserId} — skipping duplicate.", evt.UserId);
            return;
        }

        var credentials = UserCredentials.CreateStub(evt.UserId, evt.Email);
        dbContext.UserCredentials.Add(credentials);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Auth: Credentials stub created for user {UserId}.", evt.UserId);
    }
}
