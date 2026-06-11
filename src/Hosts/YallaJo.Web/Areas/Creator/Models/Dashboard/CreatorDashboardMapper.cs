namespace YallaJo.Web.Areas.Creator.Models.Dashboard;

/// <summary>
/// Pure mapping + state-resolution for the Creator dashboard. Deliberately
/// dependency-free so the gate logic can be exhaustively unit-tested without a host.
/// </summary>
public static class CreatorDashboardMapper
{
    public static CreatorDashboardVm ToVm(
        CreatorProfileMineResponse? profile,
        CreatorApplicationMineResponse? application,
        IReadOnlyList<CreatorDashboardArticleVm>? recentArticles = null,
        CreatorNeedsAttentionVm? needsAttention = null)
    {
        if (profile is not null)
            return FromProfile(profile, recentArticles ?? [], needsAttention);

        return FromApplication(application);
    }

    private static CreatorDashboardVm FromProfile(
        CreatorProfileMineResponse profile,
        IReadOnlyList<CreatorDashboardArticleVm> recentArticles,
        CreatorNeedsAttentionVm? needsAttention)
    {
        var state = ParseProfileStatus(profile.Status);

        // Stats + snapshot are only surfaced for an Active (Approved) profile.
        var approved = state == CreatorDashboardState.Approved;

        return new CreatorDashboardVm
        {
            State              = state,
            DisplayName        = profile.DisplayName,
            Slug               = profile.Slug,
            TrustTier          = profile.TrustTier,
            ArticleCount       = approved ? profile.ArticleCount : 0,
            TotalViewCount     = approved ? profile.TotalViewCount : 0,
            TotalReactionCount = approved ? profile.TotalReactionCount : 0,
            TotalCommentCount  = approved ? profile.TotalCommentCount : 0,
            FollowerCount      = approved ? profile.FollowerCount : 0,
            RecentArticles     = approved ? recentArticles : [],
            NeedsAttention     = approved ? needsAttention : null,
        };
    }

    private static CreatorDashboardVm FromApplication(CreatorApplicationMineResponse? application)
    {
        if (application is null)
            return new CreatorDashboardVm { State = CreatorDashboardState.NotApplied };

        var state = ParseApplicationStatus(application.Status);

        return new CreatorDashboardVm
        {
            State                  = state,
            AdminNote              = application.AdminNote,
            ApplicationSubmittedAt = application.CreatedAt,
            ApplicationReviewedAt  = application.ReviewedAt,
        };
    }

    // ── Status string → state (API serializes enums by member name) ────────────

    private static CreatorDashboardState ParseProfileStatus(string? status) =>
        status switch
        {
            "Active"      => CreatorDashboardState.Approved,
            "Suspended"   => CreatorDashboardState.Suspended,
            "Deactivated" => CreatorDashboardState.Deactivated,
            // Unknown/missing status: fail safe to the suspended notice rather than
            // exposing creator tools to an indeterminate profile.
            _             => CreatorDashboardState.Suspended,
        };

    private static CreatorDashboardState ParseApplicationStatus(string? status) =>
        status switch
        {
            "Draft"          => CreatorDashboardState.ApplicationDraft,
            "Pending"        => CreatorDashboardState.ApplicationPending,
            "Rejected"       => CreatorDashboardState.ApplicationRejected,
            "MoreInfoNeeded" => CreatorDashboardState.ApplicationNeedsInfo,
            // "Approved" application without a profile is a transient/edge case;
            // treat as pending so the user sees a neutral "under review" message
            // rather than article tools they cannot yet use.
            "Approved"       => CreatorDashboardState.ApplicationPending,
            _                => CreatorDashboardState.NotApplied,
        };
}
