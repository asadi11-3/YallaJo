namespace YallaJo.Web.Areas.Accounts.Models.Bookings;

/// <summary>Body for POST /api/v1/payments/initiate.</summary>
public sealed record InitiatePaymentApiRequest(Guid BookingId, string PaymentMethod, string ReturnUrl);

/// <summary>Mirrors InitiatePaymentResult.</summary>
public sealed class InitiatePaymentResponse
{
    public Guid PaymentId { get; init; }
    public string? GatewayPaymentId { get; init; }
    public string? RedirectUrl { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>Mirrors SimulatePaymentSuccessResult (dev-only completion).</summary>
public sealed class SimulatePaymentResponse
{
    public Guid PaymentId { get; init; }
    public string PaymentStatus { get; init; } = string.Empty;
}
