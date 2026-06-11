namespace YallaJo.Web.Areas.Provider.Models.Bookings;

// ── Responses (from API) ───────────────────────────────────────────────────────

/// <summary>Mirrors ProviderBookingsPage from GET /api/v1/booking/provider/bookings.</summary>
public sealed class ProviderBookingsPageResponse
{
    public List<ProviderBookingItemResponse> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public int? TotalCount { get; init; }
}

/// <summary>Mirrors ProviderBookingItemDto.</summary>
public sealed class ProviderBookingItemResponse
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid TourId { get; init; }
    public Guid AvailabilitySlotId { get; init; }
    public DateOnly? SlotDate { get; init; }
    public TimeOnly? SlotStartTime { get; init; }
    public TimeOnly? SlotEndTime { get; init; }
    public int ParticipantCount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public bool IsInstantBooking { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>[Backend] B5 — mirrors ProviderBookingStatsDto from GET /api/v1/booking/provider/bookings/stats.</summary>
public sealed class ProviderBookingStatsResponse
{
    public int Total { get; init; }
    public int Pending { get; init; }
    public int Confirmed { get; init; }
    public int Completed { get; init; }
    public int Cancelled { get; init; }
    public int Rejected { get; init; }
}

/// <summary>Mirrors TourBookingDetailDto from GET /api/v1/booking/{id} (provider can view own-tour booking).</summary>
public sealed class ProviderBookingDetailResponse
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid TourId { get; init; }
    public Guid ProviderId { get; init; }
    public Guid AvailabilitySlotId { get; init; }
    public int ParticipantCount { get; init; }
    public ProviderBookingPricingResponse? Pricing { get; init; }
    public bool IsInstantBooking { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }
    public string? SpecialRequests { get; init; }
    public ProviderBookingCancellationResponse? Cancellation { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class ProviderBookingPricingResponse
{
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<ProviderBookingLineItemResponse> LineItems { get; init; } = [];
}

public sealed class ProviderBookingLineItemResponse
{
    public string TierType { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class ProviderBookingCancellationResponse
{
    public DateTime CancelledAt { get; init; }
    public string? Reason { get; init; }
    public decimal? RefundAmount { get; init; }
}

/// <summary>Mirrors TourLookupResponse from GET /api/v1/tours/{id} (name hydration only).</summary>
public sealed class ProviderTourLookupResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

// ── Requests (to API) ──────────────────────────────────────────────────────────

public sealed record CancelBookingApiRequest(string Reason);

public sealed record RejectBookingApiRequest(string Reason);
