using Auth.Application.Caching;
using Auth.Application.Interfaces;
using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using Security.Contracts.IntegrationEvents;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Infrastructure.EventHandlers;

/// <summary>
/// Auth consumer for <see cref="UserLifecycleChangedIntegrationEvent"/>.
/// When the target <see cref="UserLifecycleChangedIntegrationEvent.To"/> is
/// not <see cref="AccountLifecycleSnapshot.Active"/>, every active session
/// and refresh token for the user is revoked and the per-user active-sessions
/// cache is invalidated. Idempotent on re-delivery via the Auth inbox.
/// <para>
/// Cache invalidation happens only AFTER successful <c>SaveChangesAsync</c>
/// so a failed commit cannot leave the cache claiming sessions are revoked
/// while the DB still shows them active.
/// </para>
/// </summary>
public sealed class UserLifecycleChangedIntegrationEventHandler(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuthInboxStore inboxStore,
    IAuthUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UserLifecycleChangedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserLifecycleChangedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<UserLifecycleChangedIntegrationEvent> notification,
        CancellationToken cancellationToken)
    {
      
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, cancellationToken))
        {
            logger.LogWarning(
                "Auth: Message {MessageId} (UserLifecycleChanged for {UserId} {From}→{To}) already processed — skipping.",
                notification.MessageId,
                notification.Event.UserId,
                notification.Event.From,
                notification.Event.To);
            return;
        }

        var userId = notification.Event.UserId;
        var to     = notification.Event.To;

        if (to == AccountLifecycleSnapshot.Active)
        {
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Auth: UserLifecycleChanged for {UserId} → Active — no session teardown required; inbox marked.",
                userId);
            return;
        }

        var activeSessions = await sessionRepository.GetAllAsync(
            filter: s => s.UserId == userId && !s.IsRevoked,
            asNoTracking: false,
            ct: cancellationToken);

        foreach (var session in activeSessions)
            session.Revoke();

        var activeTokens = await refreshTokenRepository.GetAllAsync(
            filter: rt => rt.UserId == userId && !rt.IsRevoked,
            asNoTracking: false,
            ct: cancellationToken);

        foreach (var token in activeTokens)
            token.Revoke();

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(
            AuthCacheKeys.UserSessionsTag(userId), cancellationToken);

        logger.LogInformation(
            "Auth: Revoked {SessionCount} session(s) and {TokenCount} refresh token(s) for user {UserId} after lifecycle transition {From}→{To}.",
            activeSessions.Count,
            activeTokens.Count,
            userId,
            notification.Event.From,
            to);
    }
}
