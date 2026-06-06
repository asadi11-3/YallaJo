namespace YallaJo.Web.Areas.Creator.Models.Audience;

/// <summary>
/// The creator's lifecycle state for the Audience / Followers page (CCD-7).
/// </summary>
public enum AudienceState
{
    /// <summary>No creator profile — the controller redirects to the Application flow.</summary>
    NoProfile,

    /// <summary>Profile exists but is not Active (Suspended/Deactivated) — audience data
    /// is unavailable until the profile is active.</summary>
    Unavailable,

    /// <summary>Active profile — a read-only audience overview is rendered.</summary>
    Active,
}

/// <summary>
/// Read-only view model for the Creator-area Audience page.
/// <para>
/// CRITICAL: this VM never carries raw follower user IDs or any private/internal
/// identifier. The backend <c>/followers</c> endpoint returns a bare array of
/// follower GUIDs; those are converted into anonymous, ordinal-labelled rows
/// (<see cref="AudienceFollowerRowVm"/>) so no identity can ever reach the HTML.
/// </para>
/// </summary>
public sealed class CreatorAudienceVm
{
    public AudienceState State { get; init; } = AudienceState.NoProfile;

    /// <summary>Non-Active status label (e.g. "Suspended"/"Deactivated") for the
    /// unavailable notice. Empty for the Active/NoProfile states.</summary>
    public string StatusLabel { get; init; } = string.Empty;

    /// <summary>Authoritative follower count from <c>/profile/mine</c>
    /// (<c>CreatorProfileDto.FollowerCount</c>).</summary>
    public int FollowerCount { get; init; }

    /// <summary>Anonymous follower rows for the current page (no identity rendered).</summary>
    public IReadOnlyList<AudienceFollowerRowVm> Followers { get; init; } = [];

    public bool HasFollowers => Followers.Count > 0;

    public AudiencePagerVm Pager { get; init; } = new();
}

/// <summary>
/// One anonymous follower row. Carries ONLY a display ordinal — never the
/// underlying user GUID, name, or avatar (the backend exposes none of these).
/// </summary>
public sealed class AudienceFollowerRowVm
{
    /// <summary>1-based position within the full follower ordering (page-aware).</summary>
    public int Position { get; init; }

    /// <summary>When this follower started following (Gap 3 Phase A). No identity.</summary>
    public DateTime? FollowedAt { get; init; }

    /// <summary>Anonymous label shown in the UI, e.g. "Follower #21".</summary>
    public string Label => $"Follower #{Position}";
}

/// <summary>
/// Pager for the audience list. The backend <c>/followers</c> endpoint provides no
/// total-count envelope, so "has next" is inferred from the heuristic
/// "this page returned a full <see cref="PageSize"/> worth of rows".
/// </summary>
public sealed class AudiencePagerVm
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage { get; init; }
}
