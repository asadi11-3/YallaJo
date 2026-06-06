using YallaJo.Web.Areas.Creator.Models.Dashboard;

namespace YallaJo.Web.Areas.Creator.Models.Audience;

/// <summary>
/// Pure mapping for the Audience page (CCD-7): profile state → VM, and the public-safe
/// follower summaries → anonymous, ordinal-labelled rows.
/// <para>
/// The followers endpoint now returns public-safe summaries (Gap 3 Phase A): an opaque
/// ordinal + followed-at timestamp, never any user identity. No GUID can reach the HTML.
/// </para>
/// </summary>
public static class CreatorAudienceMapper
{
    private const int DefaultPageSize = 20;

    private const string ActiveStatus = "Active";

    public static CreatorAudienceVm NoProfile() => new() { State = AudienceState.NoProfile };

    public static CreatorAudienceVm Unavailable(string? statusLabel = null) => new()
    {
        State = AudienceState.Unavailable,
        StatusLabel = statusLabel ?? string.Empty,
    };

    /// <summary>
    /// Builds the page VM from the caller's own profile and the current page of
    /// public-safe follower summaries. Returns <see cref="AudienceState.NoProfile"/>
    /// when the profile is absent and <see cref="AudienceState.Unavailable"/> when it
    /// is not Active.
    /// </summary>
    public static CreatorAudienceVm ToVm(
        CreatorProfileMineResponse? profile,
        IReadOnlyList<FollowerSummaryResponse>? followers,
        int page,
        int pageSize = DefaultPageSize)
    {
        if (profile is null)
            return NoProfile();

        if (!string.Equals(profile.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
            return Unavailable(profile.Status);

        var safePage = Math.Max(1, page);
        var safePageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var items = followers ?? [];

        // Anonymous rows: ordinal + followed-at only (Gap 3 Phase A), never any
        // identity. Position is page-aware so page 2 starts at "Follower #(pageSize+1)".
        var fallbackStartOrdinal = ((safePage - 1) * safePageSize) + 1;
        var rows = new List<AudienceFollowerRowVm>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var f = items[i];
            rows.Add(new AudienceFollowerRowVm
            {
                // Prefer the server-provided ordinal; fall back to page-derived position.
                Position   = f.Ordinal > 0 ? f.Ordinal : fallbackStartOrdinal + i,
                FollowedAt = f.FollowedAt,
            });
        }

        return new CreatorAudienceVm
        {
            State = AudienceState.Active,
            FollowerCount = profile.FollowerCount,
            Followers = rows,
            Pager = new AudiencePagerVm
            {
                PageNumber = safePage,
                PageSize = safePageSize,
                // No total-count envelope from the backend → infer "more pages" from a
                // full page of results.
                HasNextPage = items.Count == safePageSize,
            },
        };
    }
}
