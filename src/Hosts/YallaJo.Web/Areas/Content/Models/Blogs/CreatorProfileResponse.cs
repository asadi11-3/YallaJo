namespace YallaJo.Web.Areas.Content.Models.Blogs;

/// <summary>
/// Web-side projection of the public creator-profile endpoint
/// (GET /api/v1/blogs/creators/profiles/{slug}). Aligned to the public-safe
/// <c>PublicCreatorProfileDto</c> (Gap 4): the backend no longer returns UserId,
/// internal Status, LinkedProviderId, or CreatedAt on this anonymous endpoint.
/// </summary>
public sealed class CreatorProfileResponse
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public string? TrustTier { get; init; }
    public int ArticleCount { get; init; }
    public long TotalViewCount { get; init; }
    public long TotalReactionCount { get; init; }
    public long TotalCommentCount { get; init; }
    public int FollowerCount { get; init; }
}
