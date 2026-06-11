namespace YallaJo.Web.Areas.Creator.Models.Dashboard;

/// <summary>
/// The creator's lifecycle state as resolved from the (profile/mine, applications/mine)
/// read pair. Drives which dashboard variant the view renders (CCD-1 approval gate).
/// </summary>
public enum CreatorDashboardState
{
    /// <summary>No profile and no application — user has never applied.</summary>
    NotApplied,

    /// <summary>Has a Draft application (started, not submitted).</summary>
    ApplicationDraft,

    /// <summary>Application submitted and awaiting admin review.</summary>
    ApplicationPending,

    /// <summary>Application rejected by an admin.</summary>
    ApplicationRejected,

    /// <summary>Admin requested more information on the application.</summary>
    ApplicationNeedsInfo,

    /// <summary>Has an Active creator profile — full dashboard.</summary>
    Approved,

    /// <summary>Profile suspended by an admin.</summary>
    Suspended,

    /// <summary>Profile deactivated by the creator.</summary>
    Deactivated,
}

/// <summary>
/// Presentation model for the Creator dashboard. Contains only display-ready
/// values — no API/DTO types leak into the Razor view. Stats are populated only
/// in the <see cref="CreatorDashboardState.Approved"/> state and are always real
/// backend values (never fabricated).
/// </summary>
public sealed class CreatorDashboardVm
{
    public CreatorDashboardState State { get; init; } = CreatorDashboardState.NotApplied;

    // ── Identity (Approved state) ─────────────────────────────────────────────
    public string DisplayName { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? TrustTier { get; init; }

    // ── Real stats (Approved state only) ──────────────────────────────────────
    public int ArticleCount { get; init; }
    public long TotalViewCount { get; init; }
    public long TotalReactionCount { get; init; }
    public long TotalCommentCount { get; init; }
    public int FollowerCount { get; init; }

    // ── Application context (application states) ───────────────────────────────
    public string? AdminNote { get; init; }
    public DateTime? ApplicationSubmittedAt { get; init; }
    public DateTime? ApplicationReviewedAt { get; init; }

    // ── Recent-articles snapshot (Approved state only) ────────────────────────
    public IReadOnlyList<CreatorDashboardArticleVm> RecentArticles { get; init; } = [];

    // ── Needs-attention counts (Approved state only) ──────────────────────────
    /// <summary>
    /// Owner-scoped article counts that drive the "needs attention" strip. Null when
    /// the aggregate could not be loaded (the strip then renders nothing — graceful
    /// degradation, ERR3).
    /// </summary>
    public CreatorNeedsAttentionVm? NeedsAttention { get; init; }

    // ── Convenience flags for the view ────────────────────────────────────────
    public bool IsApproved => State == CreatorDashboardState.Approved;

    public bool ShowApplicationCta => State is CreatorDashboardState.NotApplied
        or CreatorDashboardState.ApplicationDraft
        or CreatorDashboardState.ApplicationRejected
        or CreatorDashboardState.ApplicationNeedsInfo;
}

/// <summary>One row in the approved-creator recent-articles snapshot.</summary>
public sealed class CreatorDashboardArticleVm
{
    public string Title { get; init; } = string.Empty;
    public string? Status { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
}

/// <summary>
/// Actionable, owner-scoped article counts surfaced above the vanity metrics so the
/// dashboard leads with work-to-do (drafts to finish, items in review, restorable
/// deletes). Each item links into the Articles list filtered by status.
/// </summary>
public sealed class CreatorNeedsAttentionVm
{
    public int Drafts { get; init; }
    public int PendingReview { get; init; }
    public int Deleted { get; init; }

    /// <summary>True when at least one bucket has items worth surfacing.</summary>
    public bool HasAny => Drafts > 0 || PendingReview > 0 || Deleted > 0;
}
