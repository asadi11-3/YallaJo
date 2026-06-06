namespace YallaJo.Web.Areas.Admin.Models.Bookings;

// ── Responses (from API) ───────────────────────────────────────────────────────

/// <summary>Mirrors AdminBookingsPage from GET /api/v1/booking/admin/all.</summary>
public sealed class AdminBookingsPageResponse
{
    public List<AdminBookingItemResponse> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public int? TotalCount { get; init; }
}

/// <summary>Mirrors AdminBookingItemDto. Traveler is UserId only (no PII); the DTO
/// returns no slot date/time, tour name, or payment status.</summary>
public sealed class AdminBookingItemResponse
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid TourId { get; init; }
    public Guid ProviderId { get; init; }
    public Guid AvailabilitySlotId { get; init; }
    public int ParticipantCount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public bool IsInstantBooking { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public decimal? RefundAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Mirrors TourBookingDetailDto from GET /api/v1/booking/{id}.</summary>
public sealed class AdminBookingDetailResponse
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid TourId { get; init; }
    public Guid ProviderId { get; init; }
    public Guid AvailabilitySlotId { get; init; }
    public int ParticipantCount { get; init; }
    public AdminBookingPricingResponse? Pricing { get; init; }
    public bool IsInstantBooking { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }
    public string? SpecialRequests { get; init; }
    public AdminBookingCancellationResponse? Cancellation { get; init; }
    public AdminBookingDisputeResponse? Dispute { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class AdminBookingDisputeResponse
{
    public DateTime DisputedAt { get; init; }
    public string Reason { get; init; } = string.Empty;
    public Guid? OpenedByUserId { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
    public Guid? ResolvedByAdminId { get; init; }
}

public sealed class AdminBookingPricingResponse
{
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<AdminBookingLineItemResponse> LineItems { get; init; } = [];
}

public sealed class AdminBookingLineItemResponse
{
    public string TierType { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class AdminBookingCancellationResponse
{
    public DateTime CancelledAt { get; init; }
    public string? Reason { get; init; }
    public decimal? RefundAmount { get; init; }
}

/// <summary>Mirrors TourLookupResponse from GET /api/v1/tours/{id} (name hydration only).</summary>
public sealed class AdminBookingTourLookupResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
