using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Overview;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Features.Notifications;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// §3.1 Customer / Tourist Dashboard — the "Overview" landing page served at
/// <c>GET /accounts</c>. SSR-first (rule R2): all primary content is rendered on first
/// paint. The five plan reads (profile · my-bookings · recommendations · favorites ·
/// unread-count) run in parallel (rule API1) by REUSING the existing per-feature
/// facades directly (Pattern A composite controller) — no dedicated OverviewFacade and
/// no new ApiClient are introduced, matching the convention used by
/// <see cref="BookingsController"/> and <see cref="SettingsController"/>.
/// <para>
/// Cache: authenticated ⇒ the global base output-cache policy is <c>NoCache()</c>, so
/// this page is <c>NoStore</c> without an explicit attribute (rule UI-PERF-C2), exactly
/// like every other Accounts controller. Perm: <c>[Authorize]</c>, API enforces
/// per-user ownership.
/// </para>
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class OverviewController : BaseController
{
    private readonly ProfileFacade _profile;
    private readonly BookingsFacade _bookings;
    private readonly RecommendationsFacade _recommendations;
    private readonly WishlistFacade _wishlist;
    private readonly NotificationsFacade _notifications;

    public OverviewController(
        ProfileFacade profile,
        BookingsFacade bookings,
        RecommendationsFacade recommendations,
        WishlistFacade wishlist,
        NotificationsFacade notifications)
    {
        _profile = profile;
        _bookings = bookings;
        _recommendations = recommendations;
        _wishlist = wishlist;
        _notifications = notifications;
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AccountNav"] = "Overview";

        // API1: fire all reads together; each sub-read degrades independently so one
        // failing section never 500s the page (ERR2/ERR4).
        var profileTask = _profile.GetAsync(ct);
        var bookingsTask = _bookings.GetBookingsAsync("Upcoming", ct: ct);
        var recsTask = _recommendations.GetAsync(ct);
        var favoritesTask = _wishlist.GetWishlistAsync(ct);
        var bellTask = _notifications.GetBellAsync(ct);

        await Task.WhenAll(profileTask, bookingsTask, recsTask, favoritesTask, bellTask);

        var profileResult = await profileTask;
        var bookingsResult = await bookingsTask;
        var recsResult = await recsTask;
        var favoritesResult = await favoritesTask;
        var bell = await bellTask;

        // A 401 on the (auth-required) profile read means the session is gone: bounce to
        // login rather than rendering an empty shell.
        if (GuardSignOut(profileResult) is { } signOut) return signOut;

        // Populate the shared account sidebar (avatar / name / email).
        var profile = profileResult.Data;
        ViewBag.Sidebar = new AccountSidebarVm
        {
            AvatarUrl = profile?.AvatarUrl,
            DisplayName = string.IsNullOrWhiteSpace(profile?.DisplayName)
                ? $"{profile?.FirstName} {profile?.LastName}".Trim()
                : profile!.DisplayName,
            Email = profile?.Email,
        };

        var vm = OverviewMapper.Build(
            profile,
            bookingsResult.IsSuccess ? bookingsResult.Data : null,
            favoritesResult.IsSuccess ? favoritesResult.Data : null,
            recsResult.IsSuccess ? recsResult.Data : null,
            bell.UnreadCount);

        return View(vm);
    }
}
