namespace YallaJo.Web.Areas.Admin.Models.Bookings;

public sealed class AdminBookingsPageResponse
{
    public IReadOnlyList<AdminBookingItemResponse> Items { get; set; } = [];
    public string? NextCursor { get; set; }
    public int? TotalCount { get; set; }
}

public sealed class AdminBookingItemResponse
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid TourId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid AvailabilitySlotId { get; set; }
    public int ParticipantCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsInstantBooking { get; set; }
    public DateTime PaymentExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
