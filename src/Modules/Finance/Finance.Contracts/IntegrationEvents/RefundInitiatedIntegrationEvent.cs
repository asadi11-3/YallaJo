using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.refund.initiated.v1</c>) when a refund has been
/// initiated against an original payment. Refunds materialise as sibling Payment rows
/// with <c>PaymentType=Refund</c>. Consumers: Analytics, Messaging.
/// </summary>
public sealed record RefundInitiatedIntegrationEvent(
    Guid RefundPaymentId,
    Guid OriginalPaymentId,
    Guid BookingId,
    decimal Amount,
    string Currency,
    string Reason,
    DateTime InitiatedAt) : IntegrationEventBase;
