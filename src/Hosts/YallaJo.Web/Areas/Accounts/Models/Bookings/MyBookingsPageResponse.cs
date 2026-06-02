namespace YallaJo.Web.Areas.Accounts.Models.Bookings;

public sealed class MyBookingsPageResponse
{
    public IReadOnlyList<MyBookingItemResponse> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public int? TotalCount { get; init; }
}

public sealed class MyBookingItemResponse
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid TourId { get; init; }
    public Guid ProviderId { get; init; }
    public Guid AvailabilitySlotId { get; init; }
    public int ParticipantCount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public bool IsInstantBooking { get; init; }
    public DateTime? PaymentExpiresAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
