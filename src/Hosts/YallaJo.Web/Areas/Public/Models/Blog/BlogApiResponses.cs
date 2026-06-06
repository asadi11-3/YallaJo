namespace YallaJo.Web.Areas.Public.Models.Blog;

public sealed class BlogSummaryResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Summary { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public bool IsFeatured { get; init; }
}

public sealed class BlogDetailResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = "";
    public string Title { get; init; } = "";
    public string Content { get; init; } = "";
    public string? Summary { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public int TourCount { get; init; }
    public bool IsFeatured { get; init; }
}

public sealed class PaginatedBlogsResponse
{
    public List<BlogSummaryResponse> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

public sealed class CreatorProfileResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public string? TrustTier { get; init; }
    public int ArticleCount { get; init; }
    public long TotalViewCount { get; init; }
    public long TotalReactionCount { get; init; }
    public long TotalCommentCount { get; init; }
    public int FollowerCount { get; init; }
}
