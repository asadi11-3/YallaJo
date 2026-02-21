using Accounts.Contracts.IntegrationEvents;
using Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Infrastructure.EventHandlers;

/// <summary>
/// Activates a user's credentials when their email is verified in the Accounts module.
/// Triggered by OutboxProcessor reading an EmailVerifiedIntegrationEvent from the Accounts outbox.
/// </summary>
public sealed class EmailVerifiedIntegrationEventHandler(
    AuthDbContext dbContext,
    ILogger<EmailVerifiedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<EmailVerifiedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<EmailVerifiedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        logger.LogInformation(
            "Auth ← Accounts: EmailVerified received for user {UserId}. Activating credentials.",
            evt.UserId);

        var credentials = await dbContext.UserCredentials
            .FirstOrDefaultAsync(c => c.Id == evt.UserId, ct);

        if (credentials is null)
        {
            logger.LogWarning(
                "Auth: No credentials found for user {UserId} — cannot activate. " +
                "UserRegisteredIntegrationEvent may not have been processed yet.",
                evt.UserId);
            return;
        }

        credentials.Activate();
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Auth: Credentials activated for user {UserId}.", evt.UserId);
    }
}
