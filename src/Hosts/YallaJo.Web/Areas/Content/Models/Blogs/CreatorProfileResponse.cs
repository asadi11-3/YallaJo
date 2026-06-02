namespace YallaJo.Web.Areas.Content.Models.Blogs;

public sealed class CreatorProfileResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public string? TrustTier { get; init; }
    public string? Status { get; init; }
    public int ArticleCount { get; init; }
    public long TotalViewCount { get; init; }
    public long TotalReactionCount { get; init; }
    public long TotalCommentCount { get; init; }
    public int FollowerCount { get; init; }
    public Guid? LinkedProviderId { get; init; }
    public DateTime CreatedAt { get; init; }
}
