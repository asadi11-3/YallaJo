
using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Infrastructure.EventHandlers;


public sealed class PasswordChangedIntegrationEventHandler(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthInboxStore inboxStore,
    IAuthUnitOfWork unitOfWork,
    ILogger<PasswordChangedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PasswordChangedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PasswordChangedIntegrationEvent> notification,
        CancellationToken ct)
    {
      
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Auth: Message {MessageId} (PasswordChanged for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var userId = notification.Event.UserId;

      
        var activeSessions = await sessionRepository.GetAllAsync(
            filter: s => s.UserId == userId && !s.IsRevoked,
            asNoTracking: false,
            ct: ct);

        foreach (var session in activeSessions)
            session.Revoke();

        
        var activeTokens = await refreshTokenRepository.GetAllAsync(
            filter: rt => rt.UserId == userId && !rt.IsRevoked,
            asNoTracking: false,
            ct: ct);

        foreach (var token in activeTokens)
            token.Revoke();

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Auth: Revoked {SessionCount} sessions and {TokenCount} refresh tokens for user {UserId} after password change.",
            activeSessions.Count, activeTokens.Count, userId);
    }
}
