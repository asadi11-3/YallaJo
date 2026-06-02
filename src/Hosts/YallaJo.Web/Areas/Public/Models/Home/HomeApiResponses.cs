namespace YallaJo.Web.Areas.Public.Models.Home;

/// <summary>Lightweight tour summary returned by /tours/featured.</summary>
public sealed class FeaturedTourResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public string? Currency { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }
    public string? Status { get; init; }
}

/// <summary>Item returned by /popular/* and /trending (IDs only - must be hydrated).</summary>
public sealed class PopularEntityResponse
{
    public Guid EntityId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public double Score { get; init; }
    public int TrendingRank { get; init; }
    public int ReviewCount { get; init; }
}

/// <summary>Category returned by /content-core/categories.</summary>
public sealed class HomeCategoryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public Guid? ParentCategoryId { get; init; }
}

/// <summary>Attachment returned by /content-core/attachments.</summary>
public sealed class HomeAttachmentResponse
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public int SortOrder { get; init; }
}
