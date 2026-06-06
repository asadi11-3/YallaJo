using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>finance.refund.completed.v1</c>) when a refund has been
/// confirmed by the gateway. Consumers: Booking (final cancellation accounting),
/// Messaging (user confirmation), Analytics.
/// </summary>
/// <param name="UserId">
/// Identifier of the traveler who originally paid. Defaulted to <see cref="Guid.Empty"/> for
/// backward-compatibility with in-flight outbox messages produced before this field existed;
/// consumers MUST tolerate <c>Guid.Empty</c> and skip user-targeted notifications in that case.
/// </param>
public sealed record RefundCompletedIntegrationEvent(
    Guid RefundPaymentId,
    Guid OriginalPaymentId,
    Guid BookingId,
    decimal Amount,
    string Currency,
    string Reason,
    string GatewayRefundId,
    DateTime CompletedAt,
    Guid UserId = default) : IntegrationEventBase;
