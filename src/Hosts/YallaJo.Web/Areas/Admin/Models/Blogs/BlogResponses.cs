namespace YallaJo.Web.Areas.Admin.Models.Blogs;

/// <summary>Mirrors <c>BlogSummaryDto</c> from <c>GET /api/v1/blogs</c> (Published list).</summary>
public sealed class BlogSummaryResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public Guid? PlaceId { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
}

/// <summary>Mirrors <c>AdminBlogDetailDto</c> from <c>GET /api/v1/blogs/admin/{id}</c>.</summary>
public sealed class AdminBlogDetailResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public Guid? PlaceId { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public byte[] RowVersion { get; init; } = [];
    public bool IsFeatured { get; init; }
}

/// <summary>Mirrors <c>AdminDeletedBlogListItemDto</c> from <c>GET /api/v1/blogs/admin/deleted</c>.</summary>
public sealed class AdminDeletedBlogResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public Guid? PlaceId { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

/// <summary>Mirrors <c>CreateBlogResult</c> from <c>POST /api/v1/blogs</c>.</summary>
public sealed class CreateBlogResponse
{
    public Guid BlogId { get; init; }
    public string Slug { get; init; } = string.Empty;
}
