namespace YallaJo.Web.Areas.Creator.Models.Preview;

/// <summary>
/// The creator's lifecycle state for the public-preview page.
/// </summary>
public enum PreviewState
{
    /// <summary>No creator profile — the controller redirects to the Application flow.</summary>
    NoProfile,

    /// <summary>Profile exists but is not Active (Suspended/Deactivated), or the public
    /// endpoint returned 404 — the public preview is unavailable.</summary>
    Unavailable,

    /// <summary>Active profile — a read-only public preview is rendered.</summary>
    Active,
}

/// <summary>
/// Public-safe view model for the creator's "how visitors see my profile" preview.
/// <para>
/// CRITICAL: this VM contains ONLY fields a public visitor may see. It deliberately
/// omits the internal fields that the backend public DTO over-exposes — UserId,
/// internal Status, LinkedProviderId and CreatedAt are never mapped here, so they can
/// never reach the rendered HTML.
/// </para>
/// </summary>
public sealed class PublicProfilePreviewVm
{
    public PreviewState State { get; init; } = PreviewState.NoProfile;

    // ── Identity (public) ─────────────────────────────────────────────────────
    public string DisplayName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }

    /// <summary>Friendly trust-tier label (e.g. "Trusted") — public-safe.</summary>
    public string? TrustTier { get; init; }

    /// <summary>Public profile slug, used to build the real public profile URL.</summary>
    public string Slug { get; init; } = string.Empty;

    // ── Public metrics (returned by the public endpoint) ──────────────────────
    public int ArticleCount { get; init; }
    public int FollowerCount { get; init; }
    public long TotalViewCount { get; init; }
    public long TotalReactionCount { get; init; }
    public long TotalCommentCount { get; init; }

    // ── Published articles (public) ───────────────────────────────────────────
    public IReadOnlyList<PreviewArticleRowVm> Articles { get; init; } = [];

    public bool HasArticles => Articles.Count > 0;

    public PreviewPagerVm Pager { get; init; } = new();
}

/// <summary>One published article in the preview list. No status/author internals.</summary>
public sealed class PreviewArticleRowVm
{
    public string Slug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int ViewCount { get; init; }
    public int? ReadTimeMinutes { get; init; }
}

public sealed class PreviewPagerVm
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
