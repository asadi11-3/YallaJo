using Auth.Domain.Entities;
using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Domain.Events;

/// <summary>
/// Phase 2C-3 — domain event raised by the
/// <c>ForgotPasswordCommandHandler</c> immediately after a new
/// <see cref="PasswordResetToken"/> is issued. The corresponding
/// infrastructure-side domain-event handler translates it into a
/// <c>PasswordResetTokenIssuedIntegrationEvent</c> in the outbox so the
/// email is dispatched asynchronously, outside the command's UoW.
/// <para>
/// Carries the PLAIN reset code so the email dispatcher can include it
/// in the outgoing email body. See the integration-event record for the
/// security tradeoff.
/// </para>
/// </summary>
public sealed record PasswordResetTokenIssuedEvent(
    Guid TokenId,
    Guid UserId,
    string DeliveryAddress,
    string PlainCode,
    DateTime ExpiresAt,
    PasswordResetOrigin Origin) : DomainEventBase;
