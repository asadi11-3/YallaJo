namespace YallaJo.Web.Areas.Public.Models.Packages;

/// <summary>
/// Pagination envelope for GET /api/v1/tours/packages.
/// Mirrors the backend <c>PaginatedResult&lt;TourPackageSummaryDto&gt;</c>.
/// </summary>
public sealed class PaginatedPackagesResponse
{
    public IReadOnlyList<PackageSummaryResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>
/// One row in the public package listing. Mirrors the backend
/// <c>TourPackageSummaryDto</c> (public, no internal/admin fields).
/// </summary>
public sealed class PackageSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal PriceAmount { get; set; }
    public string Currency { get; set; } = "";
    public int? MaxParticipants { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int IncludedTourCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Full package detail for GET /api/v1/tours/packages/{id}.
/// Mirrors the backend <c>TourPackageDetailDto</c>.
/// </summary>
public sealed class PackageDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal PriceAmount { get; set; }
    public string Currency { get; set; } = "";
    public int? MaxParticipants { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<PackageIncludedTourResponse> IncludedTours { get; set; } = [];
    public IReadOnlyList<PackageInclusionResponse> Inclusions { get; set; } = [];
}

/// <summary>
/// A tour included in a package. Mirrors the backend <c>TourSummaryDto</c>.
/// </summary>
public sealed class PackageIncludedTourResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = "";
    public decimal? SalePrice { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int BookingCount { get; set; }
    public bool IsFeatured { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// One inclusion line on a package. Mirrors the backend <c>TourPackageInclusionDto</c>.
/// </summary>
public sealed class PackageInclusionResponse
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public int SortOrder { get; set; }
}
