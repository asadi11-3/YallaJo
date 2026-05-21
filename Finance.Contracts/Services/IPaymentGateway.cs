namespace Finance.Contracts.Services;

/// <summary>
/// Abstraction over external payment processors (Stripe, HyperPay, Fake-in-test).
/// Implementations MUST NOT log full card numbers, CVV, or full bank account numbers.
/// All sensitive fields must be redacted via <c>PaymentRedactor</c> before passing
/// through logs or telemetry. Implementations are expected to be stateless and are
/// registered as <c>Singleton</c>.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Stable identifier of the gateway implementation, e.g. <c>"Fake"</c>, <c>"Stripe"</c>, <c>"HyperPay"</c>.</summary>
    string GatewayName { get; }

    /// <summary>Initiates a payment with the gateway and returns the gateway transaction handle.</summary>
    Task<InitiateResult> InitiateAsync(InitiateRequest request, CancellationToken ct);

    /// <summary>Verifies the HMAC signature of an inbound webhook. Implementations must reject on header miss.</summary>
    Task<bool> VerifyWebhookSignatureAsync(string rawBody, IDictionary<string, string> headers, CancellationToken ct);

    /// <summary>Submits a refund against a previously completed gateway payment.</summary>
    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct);

    /// <summary>Submits a payout (outbound disbursement) to a provider's bank account ref.</summary>
    Task<PayoutResult> PayoutAsync(PayoutRequest request, CancellationToken ct);
}

/// <summary>Request envelope for <see cref="IPaymentGateway.InitiateAsync"/>.</summary>
public sealed record InitiateRequest(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string DescribedAs,
    Uri ReturnUrl,
    IDictionary<string, string> Metadata);

/// <summary>Response envelope from <see cref="IPaymentGateway.InitiateAsync"/>.</summary>
public sealed record InitiateResult(
    string GatewayPaymentId,
    Uri? RedirectUrl,
    string? ClientSecret);

/// <summary>Request envelope for <see cref="IPaymentGateway.RefundAsync"/>.</summary>
public sealed record RefundRequest(
    string OriginalGatewayPaymentId,
    decimal Amount,
    string Currency,
    string Reason);

/// <summary>Response envelope from <see cref="IPaymentGateway.RefundAsync"/>.</summary>
public sealed record RefundResult(
    string GatewayRefundId,
    RefundStatus Status,
    string? FailureCode);

/// <summary>Request envelope for <see cref="IPaymentGateway.PayoutAsync"/>.</summary>
public sealed record PayoutRequest(
    Guid PayoutId,
    string ProviderBankAccountRef,
    decimal Amount,
    string Currency);

/// <summary>Response envelope from <see cref="IPaymentGateway.PayoutAsync"/>.</summary>
public sealed record PayoutResult(
    string GatewayPayoutId,
    PayoutGatewayStatus Status,
    string? FailureCode);

/// <summary>Terminal status returned by <see cref="IPaymentGateway.RefundAsync"/>.</summary>
public enum RefundStatus
{
    Completed,
    Pending,
    Failed
}

/// <summary>Terminal status returned by <see cref="IPaymentGateway.PayoutAsync"/>.</summary>
public enum PayoutGatewayStatus
{
    Completed,
    Pending,
    Failed
}
