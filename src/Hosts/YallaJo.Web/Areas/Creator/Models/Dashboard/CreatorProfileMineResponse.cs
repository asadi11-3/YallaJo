namespace YallaJo.Web.Areas.Creator.Models.Dashboard;

/// <summary>
/// Web-side projection of the backend <c>CreatorProfileDto</c> returned by
/// <c>GET /api/v1/blogs/creators/profile/mine</c>.
/// <para>
/// Enums (<c>Status</c>, <c>TrustTier</c>) are deserialized as strings because the
/// API serializes enums via <c>JsonStringEnumConverter</c> (member name as-is, e.g.
/// "Active") and the Web <c>ApiClient</c> has no enum converter on read. This mirrors
/// the existing <c>Content.Models.Blogs.CreatorProfileResponse</c> convention.
/// </para>
/// </summary>
public sealed class CreatorProfileMineResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }

    /// <summary>"New" | "Trusted" | "Expert".</summary>
    public string? TrustTier { get; init; }

    /// <summary>"Active" | "Suspended" | "Deactivated".</summary>
    public string? Status { get; init; }

    public int ArticleCount { get; init; }
    public long TotalViewCount { get; init; }
    public long TotalReactionCount { get; init; }
    public long TotalCommentCount { get; init; }
    public int FollowerCount { get; init; }
    public Guid? LinkedProviderId { get; init; }
    public DateTime CreatedAt { get; init; }
}
