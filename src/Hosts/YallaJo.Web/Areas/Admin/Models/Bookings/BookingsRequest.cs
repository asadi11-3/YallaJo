namespace YallaJo.Web.Areas.Admin.Models.Bookings;

public sealed class BookingsFilterRequest
{
    public string? Status { get; set; }
    public string? FromDate { get; set; }
    public string? ToDate { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? TourId { get; set; }
    public string? Cursor { get; set; }
    public int PageSize { get; set; } = 25;
}
