namespace YallaJo.Web.Areas.Public.Models.Booking;

// ---- Requests (outbound to API) ----

public sealed record CreateTourBookingRequest(
    Guid TourId,
    Guid? GuideId,
    Guid AvailabilitySlotId,
    ParticipantBreakdownRequest ParticipantBreakdown,
    bool IsPrivate,
    string? PromoCode,
    int LoyaltyPointsToRedeem,
    string? SpecialRequests);

public sealed record ParticipantBreakdownRequest(int Adult, int Child, int Infant, int Senior);

// ---- Responses (inbound from API) ----

public sealed class BookingPricingResponse
{
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal LoyaltyAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public IReadOnlyList<BookingLineItemResponse> LineItems { get; init; } = [];
}

public sealed class BookingLineItemResponse
{
    public string? TierType { get; init; }
    public int Count { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class CreateTourBookingResponse
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid TourId { get; init; }
    public Guid AvailabilitySlotId { get; init; }
    public int ParticipantCount { get; init; }
    public BookingPricingResponse? Pricing { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }
    public string? PaymentToken { get; init; }
}

public sealed class TourBookingDetailResponse
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid TourId { get; init; }
    public Guid? ProviderId { get; init; }
    public Guid AvailabilitySlotId { get; init; }
    public int ParticipantCount { get; init; }
    public BookingPricingResponse? Pricing { get; init; }
    public bool IsInstantBooking { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }
    public string? SpecialRequests { get; init; }
    public BookingCancellationResponse? Cancellation { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class BookingCancellationResponse
{
    public DateTime CancelledAt { get; init; }
    public string? Source { get; init; }
    public string? Reason { get; init; }
    public decimal? RefundAmount { get; init; }
}

// ---- Payment ----

public sealed record InitiatePaymentRequest(Guid BookingId, string PaymentMethod, string? ReturnUrl);

public sealed class InitiatePaymentResponse
{
    public Guid PaymentId { get; init; }
    public string? GatewayPaymentId { get; init; }
    public string? RedirectUrl { get; init; }
    public string? ClientSecret { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
