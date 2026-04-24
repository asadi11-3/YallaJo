using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Contracts.IntegrationEvents;

/// <summary>
/// Phase 2C-3 — outbox-dispatched integration event emitted whenever a
/// new password-reset token has been issued. The email dispatch handler
/// in <c>Auth.Infrastructure</c> consumes this event, sends the reset
/// email via <c>IEmailService</c>, and updates the token's
/// <c>DeliveryStatus</c> accordingly.
/// <para>
/// <b>⚠ Security warning — plain reset code in payload.</b> Because the
/// aggregate only persists <c>TokenHash</c>, the plain reset code is
/// embedded directly in the <c>OutboxMessage.Content</c> JSON so the
/// downstream handler can include it in the outgoing email. Same
/// tradeoff and mitigations as
/// <see cref="ActivationTokenIssuedIntegrationEvent"/>; see that record
/// for the full rationale. The reset code's 10-minute lifetime narrows
/// the exposure window considerably compared to activation tokens.
/// </para>
/// </summary>
public sealed record PasswordResetTokenIssuedIntegrationEvent(
    Guid TokenId,
    Guid UserId,
    string DeliveryAddress,
    string PlainCode,
    DateTime ExpiresAt,
    PasswordResetOriginSnapshot Origin) : IntegrationEventBase;

/// <summary>
/// Cross-module-safe projection of
/// <c>Auth.Domain.Entities.PasswordResetOrigin</c>. Defined in contracts
/// so consumers (future Auth / Accounts telemetry subscribers) don't
/// need a reference on <c>Auth.Domain</c>. Ordinals are guaranteed 1:1
/// with the domain enum.
/// </summary>
public enum PasswordResetOriginSnapshot
{
    SelfService    = 0,
    AdminInitiated = 1,
    Reassignment   = 2,
}
