namespace YallaJo.Web.Areas.Public.Models.Packages;

/// <summary>Public list-item payload for GET /api/v1/tours/packages (mirrors TourPackageSummaryDto).</summary>
public sealed class PackageSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string Currency { get; init; } = "";
    public int? MaxParticipants { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public int IncludedTourCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Public detail payload for GET /api/v1/tours/packages/{id} (mirrors TourPackageDetailDto).</summary>
public sealed class PackageDetailResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string Currency { get; init; } = "";
    public int? MaxParticipants { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<PackageIncludedTourResponse> IncludedTours { get; init; } = [];
    public List<PackageInclusionResponse> Inclusions { get; init; } = [];
}

/// <summary>One inclusion line within a package (mirrors TourPackageInclusionDto).</summary>
public sealed class PackageInclusionResponse
{
    public Guid Id { get; init; }
    public string Description { get; init; } = "";
    public int SortOrder { get; init; }
}

/// <summary>One included tour within a package (mirrors TourSummaryDto).</summary>
public sealed class PackageIncludedTourResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = "";
    public decimal? SalePrice { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }
    public string? Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Paged wrapper for the package listing (tolerant of the API PaginatedResult shape).</summary>
public sealed class PaginatedPackagesResponse
{
    public List<PackageSummaryResponse> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
