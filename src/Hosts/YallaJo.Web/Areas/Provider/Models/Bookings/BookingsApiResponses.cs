namespace YallaJo.Web.Areas.Provider.Models.Bookings;

public sealed class JoinRequestResponse
{
    public Guid Id { get; init; }
    public Guid TourBookingId { get; init; }
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public string? Message { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public string? ResponseMessage { get; init; }
    public Guid? ResultingBookingId { get; init; }
    public DateTime CreatedAt { get; init; }
}

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
    public BookingPricingResponse? Pricing { get; init; }
    public bool IsInstantBooking { get; init; }
    public DateTime PaymentExpiresAt { get; init; }
    public string? SpecialRequests { get; init; }
    public BookingConfirmationResponse? Confirmation { get; init; }
    public BookingRejectionResponse? Rejection { get; init; }
    public BookingCancellationResponse? Cancellation { get; init; }
    public BookingCompletionResponse? Completion { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class BookingPricingResponse
{
    public decimal Subtotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal LoyaltyAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<BookingLineItemResponse> LineItems { get; init; } = [];
}

public sealed class BookingLineItemResponse
{
    public string? TierType { get; init; }
    public int Count { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class BookingConfirmationResponse
{
    public DateTime ConfirmedAt { get; init; }
    public string? Source { get; init; }
}

public sealed class BookingRejectionResponse
{
    public DateTime RejectedAt { get; init; }
    public string? Reason { get; init; }
}

public sealed class BookingCancellationResponse
{
    public DateTime CancelledAt { get; init; }
    public string? Source { get; init; }
    public string? Reason { get; init; }
    public decimal? RefundAmount { get; init; }
}

public sealed class BookingCompletionResponse
{
    public DateTime CompletedAt { get; init; }
    public Guid? CompletedByUserId { get; init; }
}

public sealed record ApproveJoinRequestRequest(string? ResponseMessage);

public sealed record RejectJoinRequestRequest(string? ResponseMessage);

public sealed record RejectTourBookingRequest(string Reason);
