using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.refund.failed.v1</c>) when a refund attempt
/// has failed. After <c>MaxRetries</c> attempts, the refund is considered finally
/// failed and operations must reconcile manually. Consumers: Messaging (admin alert),
/// Analytics.
/// </summary>
public sealed record RefundFailedIntegrationEvent(
    Guid RefundPaymentId,
    Guid OriginalPaymentId,
    Guid BookingId,
    decimal Amount,
    string Currency,
    string Reason,
    int AttemptCount,
    string FailureReason,
    DateTime FailedAt) : IntegrationEventBase;
