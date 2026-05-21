using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

/// <summary>
/// Raised when an administrator approves a payout for execution against the gateway.
/// Required for payouts whose net amount exceeds the configured large-payout threshold.
/// </summary>
/// <param name="PayoutId">Identifier of the approved payout batch.</param>
/// <param name="ApprovedByUserId">Identifier of the admin user that approved the payout.</param>
/// <param name="ApprovedAt">UTC instant of approval.</param>
/// <param name="NetAmount">Net amount payable to the provider.</param>
/// <param name="Currency">3-letter ISO-4217 currency code.</param>
/// <param name="ProviderId">Identifier of the receiving provider.</param>
public sealed record PayoutApprovedDomainEvent(
    Guid PayoutId,
    Guid ApprovedByUserId,
    DateTime ApprovedAt,
    decimal NetAmount,
    string Currency,
    Guid ProviderId) : DomainEventBase;
