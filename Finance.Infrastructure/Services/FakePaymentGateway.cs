using Finance.Contracts.Services;

namespace Finance.Infrastructure.Services;

/// <summary>
/// Development / test implementation of <see cref="IPaymentGateway"/>.
/// Always succeeds with a deterministic gateway transaction id derived from the idempotency key.
/// MUST NOT be registered in Production DI.
/// </summary>
internal sealed class FakePaymentGateway : IPaymentGateway
{
    public Task<PaymentChargeResult> ChargeAsync(PaymentChargeRequest request, CancellationToken ct = default)
    {
        var txId = $"fake_charge_{request.IdempotencyKey:N}";
        return Task.FromResult(new PaymentChargeResult(
            IsSuccess: true,
            GatewayTransactionId: txId,
            FailureReason: null,
            GatewayResponseRaw: "{\"status\":\"ok\",\"gateway\":\"fake\"}"));
    }

    public Task<PaymentRefundResult> RefundAsync(PaymentRefundRequest request, CancellationToken ct = default)
    {
        var refundId = $"fake_refund_{request.IdempotencyKey:N}";
        return Task.FromResult(new PaymentRefundResult(
            IsSuccess: true,
            RefundTransactionId: refundId,
            FailureReason: null));
    }

    public Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, CancellationToken ct = default)
        => Task.FromResult(true);
}
