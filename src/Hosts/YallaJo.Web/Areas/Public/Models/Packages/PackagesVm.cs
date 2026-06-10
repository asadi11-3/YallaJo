using YallaJo.Web.Areas.Public.Models.Shared;

namespace YallaJo.Web.Areas.Public.Models.Packages;

/// <summary>One package card in the public packages grid.</summary>
public sealed class PackageCardVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string Currency { get; init; } = "";
    public int IncludedTourCount { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public bool HasValidity => ValidFrom is not null || ValidTo is not null;
}

/// <summary>The public packages listing page view model (paged grid).</summary>
public sealed class PackagesGridVm
{
    public IReadOnlyList<PackageCardVm> Packages { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasResults => Packages.Count > 0;
}

/// <summary>The public package detail page view model.</summary>
public sealed class PackageDetailVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string Currency { get; init; } = "";
    public int? MaxParticipants { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }

    /// <summary>The tours bundled in this package, rendered with the shared Home tour card.</summary>
    public IReadOnlyList<TourCardVm> IncludedTours { get; init; } = [];

    /// <summary>Free-text inclusion lines, ordered by the backend SortOrder.</summary>
    public IReadOnlyList<string> Inclusions { get; init; } = [];
    public bool HasIncludedTours => IncludedTours.Count > 0;
    public bool HasInclusions => Inclusions.Count > 0;
    public bool HasValidity => ValidFrom is not null || ValidTo is not null;
}
