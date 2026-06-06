using YallaJo.Web.Infrastructure.Seo;

namespace YallaJo.Web.Areas.Public.Models.Blog;

public sealed class BlogCardVm
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Summary { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public int ViewCount { get; init; }
    public bool IsFeatured { get; init; }
}

public sealed class BlogsGridVm
{
    public IReadOnlyList<BlogCardVm> Posts { get; init; } = [];
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalCount { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasResults => Posts.Count > 0;
}

public sealed class BlogPostVm
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = "";
    public string Title { get; init; } = "";
    public string Content { get; init; } = "";
    public string? Summary { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int? ReadTimeMinutes { get; init; }
    public int ViewCount { get; init; }
    public BlogCommentListVm Comments { get; set; } = new();
    public SeoContent? Seo { get; set; }
    public bool HasReadTime => ReadTimeMinutes is > 0;
}

public sealed class CreatorProfileVm
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public int ArticleCount { get; init; }
    public long TotalViewCount { get; init; }
    public int FollowerCount { get; init; }
    public bool IsFollowing { get; set; }
    public IReadOnlyList<BlogCardVm> Posts { get; set; } = [];
    public SeoContent? Seo { get; set; }
    public bool HasBio => !string.IsNullOrWhiteSpace(Bio);
    public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarUrl);
    public bool HasPosts => Posts.Count > 0;
}
