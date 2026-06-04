namespace YallaJo.Web.Areas.Admin.Models.Bookings;

public sealed class BookingsVm
{
    public IReadOnlyList<BookingRowVm> Bookings { get; set; } = [];
    public string? NextCursor { get; set; }
    public int? TotalCount { get; set; }
    public string? StatusFilter { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
}

public sealed class BookingRowVm
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid TourId { get; set; }
    public Guid ProviderId { get; set; }
    public int ParticipantCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CanForceRefund { get; set; }
}
