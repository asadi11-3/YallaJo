namespace YallaJo.Web.Areas.Public.Models.Tours;

/// <summary>Mirrors the API TourSummaryDto returned by /tours and /tours/featured.</summary>
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
    public string? Status { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Real primary tour image (relative /uploads path) for the card; null when the tour has no images.</summary>
    public string? PrimaryImageUrl { get; init; }
}

/// <summary>Mirrors the API PaginatedResult&lt;T&gt; wrapper.</summary>
public sealed class PaginatedToursResponse
{
    public IReadOnlyList<TourSummaryResponse> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

/// <summary>Mirrors the API CategoryDto from /content-core/categories.</summary>
public sealed class CategoryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public Guid? ParentCategoryId { get; init; }
}

/// <summary>Mirrors the API AttachmentDto from /content-core/attachments.</summary>
public sealed class TourAttachmentResponse
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public int SortOrder { get; init; }
}
