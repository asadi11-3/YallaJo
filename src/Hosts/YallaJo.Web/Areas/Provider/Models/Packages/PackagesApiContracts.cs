namespace YallaJo.Web.Areas.Provider.Models.Packages;

// GET /api/v1/tours/packages — paged list (mirrors the API PaginatedResult shape).
public sealed class PackageListResponse
{
    public List<PackageSummaryResponse> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

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

// GET /api/v1/tours/packages/{id}
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

public sealed class PackageInclusionResponse
{
    public Guid Id { get; init; }
    public string Description { get; init; } = "";
    public int SortOrder { get; init; }
}

public sealed class PackageIncludedTourResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Slug { get; init; } = "";
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = "";
    public string? Status { get; init; }
}

// POST /api/v1/tours/packages
public sealed record CreateTourPackageApiRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    IReadOnlyCollection<Guid> IncludedTourIds,
    IReadOnlyCollection<string> Inclusions);

// POST /api/v1/tours/packages/{id}/inclusions
public sealed record AddPackageInclusionApiRequest(string Description);
