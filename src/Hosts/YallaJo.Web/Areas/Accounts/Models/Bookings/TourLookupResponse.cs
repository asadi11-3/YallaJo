namespace YallaJo.Web.Areas.Accounts.Models.Bookings;

public sealed class TourLookupResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
}

public sealed class AttachmentResponse
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public int SortOrder { get; init; }
}
