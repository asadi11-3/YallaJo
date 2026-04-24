using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Domain.Events;

/// <summary>
/// Phase 2C-3 — domain event raised by the
/// <c>SendActivationEmailCommandHandler</c> immediately after a new
/// <see cref="Auth.Domain.Entities.ActivationToken"/> is issued and its
/// activation link is built. The corresponding infrastructure-side
/// domain-event handler translates it into an
/// <c>ActivationTokenIssuedIntegrationEvent</c> in the outbox so the
/// email is dispatched asynchronously, outside the command's UoW.
/// <para>
/// Carries the PLAIN token + pre-built activation link so the email
/// dispatcher can include them in the outgoing email. See the
/// integration-event record for the security tradeoff.
/// </para>
/// </summary>
public sealed record ActivationTokenIssuedEvent(
    Guid TokenId,
    Guid UserId,
    string DeliveryAddress,
    string PlainToken,
    string ActivationLink,
    DateTime ExpiresAt) : DomainEventBase;
