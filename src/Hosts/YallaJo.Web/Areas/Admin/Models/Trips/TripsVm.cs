namespace YallaJo.Web.Areas.Admin.Models.Trips;

/// <summary>View model for the Tour approvals page (lookup-by-id detail + supplementary browse list).</summary>
public sealed class TripsVm
{
    public IReadOnlyList<TourRowVm> Tours { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public Guid? LookupId { get; set; }
    public TourDetailVm? Detail { get; set; }
}

public sealed class TourRowVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int BookingCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class TourDetailVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? SalePrice { get; set; }
    public int DurationMinutes { get; set; }
    public int MaxGroupSize { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int BookingCount { get; set; }
    public bool IsFeatured { get; set; }
    public Guid? PlaceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RowVersionBase64 { get; set; }
}
