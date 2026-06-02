namespace YallaJo.Web.Areas.Public.Models.Directory;

public sealed class BusinessSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? BusinessType { get; init; }
    public string? Status { get; init; }
    public double? Lat { get; init; }
    public double? Lng { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsVerified { get; init; }
    public bool IsFeatured { get; init; }
    public string? PrimaryImageUrl { get; init; }
}

public sealed class PaginatedBusinessesResponse
{
    public IReadOnlyList<BusinessSummaryResponse> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

public sealed class DirectoryAttachmentResponse
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public int SortOrder { get; init; }
}
