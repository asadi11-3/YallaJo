using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.payout.scheduled.v1</c>) when a payout batch
/// has been created and is queued for processing (either awaiting admin approval
/// for large amounts or scheduled to auto-trigger for small amounts). Consumers:
/// Messaging (provider notification), Analytics.
/// </summary>
public sealed record PayoutScheduledIntegrationEvent(
    Guid PayoutId,
    Guid ProviderId,
    DateOnly BatchPeriodStart,
    DateOnly BatchPeriodEnd,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency,
    int ItemCount,
    string Status,
    DateTime ScheduledAt) : IntegrationEventBase;
