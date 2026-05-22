using System.Security.Cryptography;
using System.Text;
using Finance.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finance.Infrastructure.Gateways;

/// <summary>
/// Deterministic, in-process implementation of <see cref="IPaymentGateway"/> intended
/// for dev, staging and tests. Real production gateways (Stripe, HyperPay) plug in
/// via the same abstraction.
/// <para>
/// Behaviour:
/// <list type="bullet">
///   <item><description><c>InitiateAsync</c> returns a deterministic <c>gw-pay-{paymentId}</c> id.</description></item>
///   <item><description><c>VerifyWebhookSignatureAsync</c> validates an <c>X-Signature: hmac-sha256=&lt;hex&gt;</c> header
///   computed against the configured shared secret. Missing/empty headers are rejected.</description></item>
///   <item><description><c>RefundAsync</c> returns an instant <c>Completed</c> refund.</description></item>
///   <item><description><c>PayoutAsync</c> returns an instant <c>Completed</c> payout.</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class FakePaymentGateway : IPaymentGateway
{
    private const string SignatureHeaderName = "X-Signature";
    private const string SignaturePrefix = "hmac-sha256=";

    private readonly FakePaymentGatewayOptions _options;
    private readonly ILogger<FakePaymentGateway> _logger;

    public FakePaymentGateway(
        IOptions<FakePaymentGatewayOptions> options,
        ILogger<FakePaymentGateway> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GatewayName => "Fake";

    public Task<InitiateResult> InitiateAsync(InitiateRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gatewayPaymentId = $"gw-pay-{request.PaymentId:N}";
        return Task.FromResult(new InitiateResult(
            GatewayPaymentId: gatewayPaymentId,
            RedirectUrl: request.ReturnUrl,
            ClientSecret: null));
    }

    public Task<bool> VerifyWebhookSignatureAsync(string rawBody, IDictionary<string, string> headers, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(headers);
        if (string.IsNullOrEmpty(rawBody))
        {
            return Task.FromResult(false);
        }

        if (!headers.TryGetValue(SignatureHeaderName, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
        {
            _logger.LogWarning("Webhook missing {Header}", SignatureHeaderName);
            return Task.FromResult(false);
        }

        if (!headerValue.StartsWith(SignaturePrefix, StringComparison.Ordinal))
        {
            _logger.LogWarning("Webhook signature header has unexpected prefix");
            return Task.FromResult(false);
        }

        var providedHex = headerValue.AsSpan(SignaturePrefix.Length).ToString();
        var secret = _options.WebhookSecret;
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogError("FakePaymentGateway: WebhookSecret is not configured");
            return Task.FromResult(false);
        }

        Span<byte> computed = stackalloc byte[HMACSHA256.HashSizeInBytes];
        var written = HMACSHA256.HashData(
            key: Encoding.UTF8.GetBytes(secret),
            source: Encoding.UTF8.GetBytes(rawBody),
            destination: computed);

        var computedHex = Convert.ToHexString(computed[..written]);
        var match = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computedHex),
            Encoding.ASCII.GetBytes(providedHex));

        if (!match)
        {
            _logger.LogWarning("Webhook signature mismatch");
        }

        return Task.FromResult(match);
    }

    public Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var refundId = $"gw-refund-{Guid.NewGuid():N}";
        return Task.FromResult(new RefundResult(
            GatewayRefundId: refundId,
            Status: RefundStatus.Completed,
            FailureCode: null));
    }

    public Task<PayoutResult> PayoutAsync(PayoutRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var payoutId = $"gw-payout-{request.PayoutId:N}";
        return Task.FromResult(new PayoutResult(
            GatewayPayoutId: payoutId,
            Status: PayoutGatewayStatus.Completed,
            FailureCode: null));
    }
}

/// <summary>Configuration for <see cref="FakePaymentGateway"/>. Bound from <c>Finance:Gateway</c>.</summary>
public sealed class FakePaymentGatewayOptions
{
    public const string SectionName = "Finance:Gateway";

    /// <summary>Provider key. Switches DI to real gateway when not <c>"Fake"</c>.</summary>
    public string Provider { get; set; } = "Fake";

    /// <summary>Shared HMAC-SHA256 secret used to verify inbound webhook signatures.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Hosts allowed in <c>InitiateRequest.ReturnUrl</c> (T1 validation).</summary>
    public IReadOnlyList<string> ReturnUrlAllowedHosts { get; set; } = [];
}
