namespace YallaJo.Web.Areas.Admin.Models.Trips;

/// <summary>Single page of approved tours from GET /api/v1/tours/ (PaginatedResult&lt;TourSummaryDto&gt;).</summary>
public sealed class TourPageResponse
{
    public IReadOnlyList<TourSummaryResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>Mirrors ContentTours TourSummaryDto (Status is a string here).</summary>
public sealed class TourSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? SalePrice { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int BookingCount { get; set; }
    public bool IsFeatured { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Mirrors ContentTours TourDetailDto (subset). Status is the TourStatus enum (0-5).
/// RowVersion (byte[] on the API) arrives as a base64 string for privileged callers.</summary>
public sealed class TourDetailResponse
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
    public int Status { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int BookingCount { get; set; }
    public bool IsFeatured { get; set; }
    public Guid? PlaceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}
