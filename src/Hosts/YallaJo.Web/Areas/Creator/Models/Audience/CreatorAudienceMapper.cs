using YallaJo.Web.Areas.Creator.Models.Dashboard;

namespace YallaJo.Web.Areas.Creator.Models.Audience;

/// <summary>
/// Pure mapping for the Audience page (CCD-7): profile state → VM, and the bare
/// follower-GUID array → anonymous, ordinal-labelled rows.
/// <para>
/// The raw follower GUIDs are used ONLY to count rows and derive ordinals — they are
/// never copied into the VM, so they cannot reach the rendered HTML.
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
    /// follower GUIDs. Returns <see cref="AudienceState.NoProfile"/> when the profile
    /// is absent and <see cref="AudienceState.Unavailable"/> when it is not Active.
    /// </summary>
    public static CreatorAudienceVm ToVm(
        CreatorProfileMineResponse? profile,
        IReadOnlyList<Guid>? followerIds,
        int page,
        int pageSize = DefaultPageSize)
    {
        if (profile is null)
            return NoProfile();

        if (!string.Equals(profile.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
            return Unavailable(profile.Status);

        var safePage = Math.Max(1, page);
        var safePageSize = pageSize <= 0 ? DefaultPageSize : pageSize;
        var ids = followerIds ?? [];

        // Anonymous rows: ordinal only, never the GUID. Position is page-aware so
        // page 2 starts at "Follower #(pageSize + 1)".
        var startOrdinal = ((safePage - 1) * safePageSize) + 1;
        var rows = new List<AudienceFollowerRowVm>(ids.Count);
        for (var i = 0; i < ids.Count; i++)
            rows.Add(new AudienceFollowerRowVm { Position = startOrdinal + i });

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
                HasNextPage = ids.Count == safePageSize,
            },
        };
    }
}
