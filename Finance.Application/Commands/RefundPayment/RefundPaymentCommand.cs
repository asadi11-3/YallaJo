using Finance.Application.Commands.InitiatePayment;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.RefundPayment;

/// <summary>
/// Command for POST /payments/{id}/refund.
/// Triggered by user, provider, or admin. F-R5 currency-strict, amount ≤ refundable balance.
/// </summary>
/// <param name="PaymentId">The ORIGINAL payment ID to refund (NOT the refund payment ID).</param>
public sealed record RefundPaymentCommand(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Reason,
    Guid CallerUserId,
    bool CallerIsAdmin,
    Guid? CallerProviderId) : IRequest<Result<RefundPaymentResult>>;

public sealed record RefundPaymentResult(
    Guid RefundPaymentId,
    Guid OriginalPaymentId,
    decimal Amount,
    string Currency,
    string Status,
    string? GatewayRefundId);
