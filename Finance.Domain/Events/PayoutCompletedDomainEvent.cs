using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when the gateway confirms the payout has settled successfully to the provider's bank account.
/// </summary>
/// <param name="PayoutId">Identifier of the completed payout batch.</param>
/// <param name="ProviderId">Identifier of the receiving provider.</param>
/// <param name="NetAmount">Net amount disbursed.</param>
/// <param name="Currency">3-letter ISO-4217 currency code.</param>
/// <param name="GatewayPayoutId">Authoritative gateway payout identifier.</param>
/// <param name="CompletedAt">UTC instant when the gateway confirmed completion.</param>
public sealed record PayoutCompletedDomainEvent(
    Guid PayoutId,
    Guid ProviderId,
    decimal NetAmount,
    string Currency,
    string GatewayPayoutId,
    DateTime CompletedAt) : DomainEventBase;
