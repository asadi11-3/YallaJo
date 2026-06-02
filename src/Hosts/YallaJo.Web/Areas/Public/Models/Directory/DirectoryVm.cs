namespace YallaJo.Web.Areas.Public.Models.Directory;

public sealed class BusinessCardVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? BusinessType { get; init; }
    public string? Location { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsVerified { get; init; }
    public bool IsFeatured { get; init; }
}

public sealed class BusinessTypeOptionVm
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

public sealed class DirectoryVm
{
    public IReadOnlyList<BusinessCardVm> Businesses { get; init; } = [];
    public IReadOnlyList<BusinessTypeOptionVm> BusinessTypes { get; init; } = [];
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public string? Query { get; init; }
    public string? SelectedBusinessType { get; init; }
    public string? City { get; init; }

    public bool HasResults => Businesses.Count > 0;
}
