namespace YallaJo.Web.Areas.Content.Models.Blogs;

public sealed class CreatorProfileVm
{
    public string Slug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string AvatarUrl { get; init; } = string.Empty;
    public int ArticleCount { get; init; }
    public int FollowerCount { get; init; }
    public long TotalViewCount { get; init; }
    public Guid FollowTargetId { get; init; }
    public IReadOnlyList<BlogCardVm> Blogs { get; init; } = [];

    public bool HasBlogs => Blogs.Count > 0;
}
