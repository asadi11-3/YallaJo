using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.Models.Recommendations;
using YallaJo.Web.Areas.Accounts.Models.Wishlist;

namespace YallaJo.Web.Areas.Accounts.Models.Overview;

/// <summary>
/// §3.1 Customer / Tourist Dashboard "Overview" landing page (route <c>/accounts</c>).
/// <para>
/// Composite, SSR-first view model that aggregates the five reads listed in the plan
/// (profile · my-bookings · recommendations · favorites · unread-count) via
/// <c>Task.WhenAll</c> (rule API1). It deliberately REUSES the existing per-feature
/// view models (<see cref="BookingsVm"/>, <see cref="RecommendationsVm"/>,
/// <see cref="WishlistVm"/>) rather than re-projecting them, so the overview stays in
/// sync with the dedicated pages it links to.
/// </para>
/// <para>
/// Every section is independently nullable: a failed sub-read degrades that one card to
/// a "Couldn't load — Retry" state (rule ERR2/ERR4) instead of failing the whole page.
/// </para>
/// </summary>
public sealed class OverviewVm
{
    /// <summary>Greeting name for the hero ("Welcome back, {name}").</summary>
    public string DisplayName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    /// <summary>Live unread notification count (from <c>GET /notifications/unread-count</c>).</summary>
    public int UnreadCount { get; init; }

    // ── KPI tiles ───────────────────────────────────────────────────────────────
    public int UpcomingBookingsCount { get; init; }
    public int FavoritesCount { get; init; }
    public int RecommendationsCount { get; init; }

    // ── Section previews (capped lists for the landing page) ─────────────────────

    /// <summary>Upcoming bookings preview (null when the bookings read failed).</summary>
    public IReadOnlyList<BookingCardVm>? UpcomingBookings { get; init; }

    /// <summary>Favorites preview (null when the favorites read failed).</summary>
    public IReadOnlyList<WishlistItemVm>? Favorites { get; init; }

    /// <summary>Recommendations preview (null when the recommendations read failed).</summary>
    public IReadOnlyList<RecommendationRowVm>? Recommendations { get; init; }

    // ── Per-section degradation flags (true = the underlying read failed) ─────────
    public bool BookingsFailed { get; init; }
    public bool FavoritesFailed { get; init; }
    public bool RecommendationsFailed { get; init; }
}
