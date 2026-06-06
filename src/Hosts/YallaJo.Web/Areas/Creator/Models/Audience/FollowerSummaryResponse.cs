namespace YallaJo.Web.Areas.Creator.Models.Audience;

/// <summary>
/// Web-side projection of the public-safe followers endpoint
/// (GET /api/v1/blogs/creators/profiles/{id}/followers), aligned to the backend
/// <c>FollowerSummaryDto</c> (Gap 3 Phase A).
/// <para>
/// Carries NO follower identity — only an opaque ordinal and the followed-at
/// timestamp. The raw follower user IDs are never returned by the endpoint.
/// </para>
/// </summary>
public sealed class FollowerSummaryResponse
{
    public int Ordinal { get; init; }
    public DateTime FollowedAt { get; init; }
}
