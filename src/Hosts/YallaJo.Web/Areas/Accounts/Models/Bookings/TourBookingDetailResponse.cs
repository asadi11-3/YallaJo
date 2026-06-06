namespace YallaJo.Web.Areas.Accounts.Models.Bookings;

public sealed class TourBookingDetailResponse
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid TourId { get; init; }
    public Guid ProviderId { get; init; }
    public Guid AvailabilitySlotId { get; init; }
    public int ParticipantCount { get; init; }
    public TourBookingPricingResponse? Pricing { get; init; }
    public bool IsInstantBooking { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }
    public string? SpecialRequests { get; init; }
    public TourBookingCancellationResponse? Cancellation { get; init; }
    public TourBookingCompletionResponse? Completion { get; init; }
    public TourBookingDisputeResponse? Dispute { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class TourBookingCompletionResponse
{
    public DateTime CompletedAt { get; init; }
    public Guid? CompletedByUserId { get; init; }
}

public sealed class TourBookingDisputeResponse
{
    public DateTime DisputedAt { get; init; }
    public string Reason { get; init; } = string.Empty;
    public Guid? OpenedByUserId { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
    public Guid? ResolvedByAdminId { get; init; }
}

public sealed class TourBookingPricingResponse
{
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal LoyaltyAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<TourBookingLineItemResponse> LineItems { get; init; } = [];
}

public sealed class TourBookingLineItemResponse
{
    public string TierType { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class TourBookingCancellationResponse
{
    public DateTime CancelledAt { get; init; }
    public string? Source { get; init; }
    public string? Reason { get; init; }
    public decimal? RefundAmount { get; init; }
}
