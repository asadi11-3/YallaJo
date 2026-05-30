using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.payout.completed.v1</c>) when a payout has
/// been confirmed by the gateway and funds have been disbursed to the provider.
/// Consumers: Messaging (provider receipt), Analytics, Accounts (reconciliation).
/// </summary>
public sealed record PayoutCompletedIntegrationEvent(
    Guid PayoutId,
    Guid ProviderId,
    decimal NetAmount,
    string Currency,
    string GatewayPayoutId,
    DateTime CompletedAt) : IntegrationEventBase;
