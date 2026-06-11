namespace YallaJo.Web.Areas.Provider.Models.Tours;

public sealed class ListMyToursResponse
{
    public List<TourSummaryResponse> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>[Backend] B1 mirror of GET /api/v1/tours/provider/my-tours/status-counts.</summary>
public sealed class TourStatusCountsResponse
{
    public int Draft { get; init; }
    public int Pending { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int Suspended { get; init; }
    public int Archived { get; init; }
    public int Total { get; init; }
}

public sealed class TourSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? SalePrice { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed class TourDetailResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string Difficulty { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public int MaxGroupSize { get; init; }
    public int? MinAge { get; init; }
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public decimal? MeetingPointLatitude { get; init; }
    public decimal? MeetingPointLongitude { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsInstantBooking { get; init; }
    public int CancellationPolicyHours { get; init; }
    public bool IsChildFriendly { get; init; }
    public bool IsAccessible { get; init; }
    public int? AgeRestriction { get; init; }
    public Guid? PlaceId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public byte[]? RowVersion { get; init; }
}

public sealed class CreateTourResponse
{
    public Guid TourId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}
