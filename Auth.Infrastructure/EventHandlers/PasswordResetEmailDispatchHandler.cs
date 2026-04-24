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
/// <see cref="PasswordResetTokenIssuedIntegrationEvent"/> and sends the
/// password-reset email via <see cref="IEmailService"/>. Replaces the
/// inline SMTP path that used to live in
/// <c>ForgotPasswordCommandHandler</c>.
/// <para>
/// Idempotency matrix (evaluated in order): inbox-processed → skip;
/// token missing → mark inbox + return; terminal → skip + mark inbox;
/// expired → skip + mark inbox; already Delivered → skip + mark inbox;
/// otherwise → send SMTP. Success → <see cref="PasswordResetToken.MarkDelivered"/>
/// + mark inbox. Failure → <see cref="PasswordResetToken.MarkDeliveryFailed"/>
/// (non-terminal) + rethrow for outbox retry. See
/// <c>ActivationEmailDispatchHandler</c> for the full rationale —
/// the two handlers follow an identical shape.
/// </para>
/// </summary>
public sealed class PasswordResetEmailDispatchHandler(
    IPasswordResetTokenRepository resetTokenRepository,
    IAuthInboxStore inboxStore,
    IAuthUnitOfWork unitOfWork,
    IEmailService emailService,
    ILogger<PasswordResetEmailDispatchHandler> logger)
    : INotificationHandler<IntegrationEventNotification<PasswordResetTokenIssuedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<PasswordResetTokenIssuedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var ev = notification.Event;

        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogInformation(
                "Auth: Password reset email dispatch for token {TokenId} already processed (inbox {MessageId}) — skipping.",
                ev.TokenId, notification.MessageId);
            return;
        }

        var token = await resetTokenRepository.GetByIdAsync(ev.TokenId, ct, asNoTracking: false);
        if (token is null)
        {
            logger.LogWarning(
                "Auth: Password reset email dispatch found no PasswordResetToken row for {TokenId} / user {UserId}. Marking inbox processed to stop retries.",
                ev.TokenId, ev.UserId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (token.IsTerminal)
        {
            logger.LogInformation(
                "Auth: Password reset token {TokenId} is terminal ({State} / {RevokedReason}) — skipping email dispatch.",
                token.Id, token.State, token.RevokedReason);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (token.IsExpired())
        {
            logger.LogInformation(
                "Auth: Password reset token {TokenId} expired at {ExpiresAt} before dispatch — skipping email.",
                token.Id, token.ExpiresAt);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (token.State == PasswordResetTokenState.Delivered)
        {
            logger.LogInformation(
                "Auth: Password reset token {TokenId} already delivered at {LastSentAt} — skipping duplicate dispatch.",
                token.Id, token.LastSentAt);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        try
        {
            await emailService.SendAsync(
                ev.DeliveryAddress,
                "YallaJo — Reset Your Password",
                $"Your password reset code is: {ev.PlainCode}. " +
                $"It expires at {ev.ExpiresAt:u}.",
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogError(ex,
                "Auth: SMTP failure dispatching password reset email for token {TokenId} / user {UserId}. Marking DeliveryStatus=Failed and rethrowing for outbox retry.",
                token.Id, token.UserId);

            token.MarkDeliveryFailed();
            await unitOfWork.SaveChangesAsync(ct);
            throw;
        }

        token.MarkDelivered();
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Auth: Password reset email dispatched for token {TokenId} / user {UserId} (inbox {MessageId}).",
            token.Id, token.UserId, notification.MessageId);
    }
}
