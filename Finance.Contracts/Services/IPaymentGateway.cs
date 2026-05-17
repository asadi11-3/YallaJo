namespace Finance.Contracts.Services;

/// <summary>
/// Abstraction over external payment processors (Stripe, PayPal, etc.).
/// Implementations MUST never log full card numbers, CVV, or full bank account numbers.
/// All sensitive fields must be redacted before passing through logs or telemetry.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken ct = default);
    Task<PaymentRefundResult> RefundAsync(PaymentRefundRequest request, CancellationToken ct = default);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, CancellationToken ct = default);
}

public sealed record PaymentChargeRequest(
    Guid IdempotencyKey,
    decimal Amount,
    string Currency,
    string PaymentMethodToken,
    string? Description);

public sealed record PaymentChargeResult(
    bool IsSuccess,
    string? GatewayTransactionId,
    string? FailureReason,
    string? GatewayResponseRaw);

public sealed record PaymentRefundRequest(
    Guid IdempotencyKey,
    string GatewayTransactionId,
    decimal RefundAmount,
    string Currency,
    string Reason);

public sealed record PaymentRefundResult(
    bool IsSuccess,
    string? RefundTransactionId,
    string? FailureReason);
