using Auth.Application.Interfaces;
using Auth.Contracts.IntegrationEvents;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Infrastructure.EventHandlers;

/// <summary>
/// Phase 2C-3 — consumes the outbox-dispatched
/// <see cref="ActivationTokenIssuedIntegrationEvent"/> and sends the
/// activation email via <see cref="IEmailService"/>. Replaces the
/// inline SMTP path that used to live in
/// <c>SendActivationEmailCommandHandler</c>.
/// <para>
/// Idempotency matrix (evaluated in order):
/// </para>
/// <list type="bullet">
///   <item><description>Inbox already marked processed → skip silently (standard outbox retry guard).</description></item>
///   <item><description>Token missing → log + mark inbox + return. A retry without the underlying row is not recoverable.</description></item>
///   <item><description>Token is terminal (Consumed / Revoked) → skip sending, mark inbox. The user has either already activated (via any path including the legacy Otp fallback) or an admin killed the token.</description></item>
///   <item><description>Token is expired → skip sending, mark inbox. Sending a link that cannot be redeemed would only confuse the user.</description></item>
///   <item><description>Token is already <see cref="ActivationTokenState.Delivered"/> → skip sending, mark inbox. A previous dispatch already put the email in the user's mailbox; another copy is spam, not help.</description></item>
///   <item><description>Otherwise (<see cref="ActivationTokenState.Issued"/>, not expired) → attempt SMTP. On success: <see cref="ActivationToken.MarkDelivered"/> + mark inbox. On failure: <see cref="ActivationToken.MarkDeliveryFailed"/> (non-terminal; token stays redeemable) + do NOT mark inbox + rethrow so the outbox retries.</description></item>
/// </list>
/// </summary>
public sealed class ActivationEmailDispatchHandler(
    IActivationTokenRepository activationTokenRepository,
    IAuthInboxStore inboxStore,
    IAuthUnitOfWork unitOfWork,
    IEmailService emailService,
    ILogger<ActivationEmailDispatchHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ActivationTokenIssuedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<ActivationTokenIssuedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var ev = notification.Event;

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogInformation(
                "Auth: Activation email dispatch for token {TokenId} already processed (inbox {MessageId}) — skipping.",
                ev.TokenId, notification.MessageId);
            return;
        }

        var token = await activationTokenRepository.GetByIdAsync(ev.TokenId, ct, asNoTracking: false);
        if (token is null)
        {
            logger.LogWarning(
                "Auth: Activation email dispatch found no ActivationToken row for {TokenId} / user {UserId}. Marking inbox processed to stop retries.",
                ev.TokenId, ev.UserId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (token.IsTerminal)
        {
            logger.LogInformation(
                "Auth: Activation token {TokenId} is terminal ({State} / {RevokedReason}) — skipping email dispatch.",
                token.Id, token.State, token.RevokedReason);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (token.IsExpired())
        {
            logger.LogInformation(
                "Auth: Activation token {TokenId} expired at {ExpiresAt} before dispatch — skipping email.",
                token.Id, token.ExpiresAt);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (token.State == ActivationTokenState.Delivered)
        {
            logger.LogInformation(
                "Auth: Activation token {TokenId} already delivered at {LastSentAt} — skipping duplicate dispatch.",
                token.Id, token.LastSentAt);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        // Attempt SMTP. Any failure re-throws after marking DeliveryStatus
        // so the outbox processor retries the same valid token.
        try
        {
            await emailService.SendAsync(
                ev.DeliveryAddress,
                "YallaJo — Activate your account",
                $"Click the link below to set your password and activate your account:\n\n{ev.ActivationLink}\n\n" +
                $"This link expires at {ev.ExpiresAt:u}.",
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogError(ex,
                "Auth: SMTP failure dispatching activation email for token {TokenId} / user {UserId}. Marking DeliveryStatus=Failed and rethrowing for outbox retry.",
                token.Id, token.UserId);

            token.MarkDeliveryFailed();
            // Persist the delivery-status update; do NOT mark the inbox
            // so the outbox processor marks the message failed and
            // re-delivers it up to MaxRetryCount.
            await unitOfWork.SaveChangesAsync(ct);
            throw;
        }

        token.MarkDelivered();
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Auth: Activation email dispatched for token {TokenId} / user {UserId} (inbox {MessageId}).",
            token.Id, token.UserId, notification.MessageId);
    }
}
