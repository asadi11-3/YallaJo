using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Contracts.IntegrationEvents;

/// <summary>
/// Phase 2C-3 — outbox-dispatched integration event emitted whenever a
/// new activation token has been issued. The email dispatch handler in
/// <c>Auth.Infrastructure</c> consumes this event, sends the activation
/// email via <c>IEmailService</c>, and updates the token's
/// <c>DeliveryStatus</c> accordingly.
/// <para>
/// <b>⚠ Security warning — plain token in payload.</b> Because the
/// aggregate only persists <c>TokenHash</c>, the plain activation token
/// + pre-built activation link are embedded directly in the
/// <c>OutboxMessage.Content</c> JSON so the downstream handler can
/// reproduce the exact URL the user needs to click. Consequences:
/// </para>
/// <list type="bullet">
///   <item><description>Anyone with read access to <c>auth.OutboxMessages</c> can see (and, until expiry, redeem) the activation link while the row is unprocessed.</description></item>
///   <item><description>Processed rows still retain the payload until the outbox cleanup job purges them. Ops should tighten processed-row retention on this table.</description></item>
///   <item><description>This tradeoff is ACCEPTED for Phase 2C-3 because the outbox lives in the same SQL Server database as the token aggregate — any attacker with DB read access already has far broader privileges. Processing normally completes within seconds.</description></item>
/// </list>
/// <para>
/// Future hardening (Phase 2C-4+): either encrypt the payload
/// symmetrically with a KMS-provided key, or carry only the tokenId in
/// the event and stage the plain token in a short-TTL distributed cache
/// that the dispatcher reads under a strict one-shot semantic.
/// </para>
/// </summary>
public sealed record ActivationTokenIssuedIntegrationEvent(
    Guid TokenId,
    Guid UserId,
    string DeliveryAddress,
    string PlainToken,
    string ActivationLink,
    DateTime ExpiresAt) : IntegrationEventBase;
