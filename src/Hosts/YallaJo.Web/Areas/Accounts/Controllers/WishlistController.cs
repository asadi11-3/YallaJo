using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Wishlist;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class WishlistController : BaseController
{
    private readonly WishlistFacade _wishlist;
    private readonly ProfileFacade _profile;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public WishlistController(WishlistFacade wishlist, ProfileFacade profile, IStringLocalizer<SharedResource> localizer)
    {
        _wishlist = wishlist;
        _profile = profile;
        _localizer = localizer;
    }

    // TODO(backend) Accounts plan Phase 4: add GET /api/v1/social/favorites/with-details
    // (kills the per-item N+1 hydration) and GET /api/v1/social/favorites/count
    // (WL4 navbar wishlist badge). Deferred: perf/nice-to-have, not correctness.
    [HttpGet("accounts/wishlist")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AccountNav"] = "Wishlist";
        await PopulateSidebarAsync(ct);

        var result = await _wishlist.GetWishlistAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new WishlistVm());
        }

        return View(result.Data);
    }

    [HttpPost("accounts/wishlist/remove/{entityType}/{entityId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string entityType, Guid entityId, CancellationToken ct)
    {
        var result = await _wishlist.RemoveAsync(entityType, entityId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.WishlistRemoved"]);
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>AJAX toggle for the reusable heart button on tour/place/business cards.
    /// Returns JSON { isFavorited } (or 401 for the client to redirect to sign-in).</summary>
    [HttpPost("accounts/wishlist/toggle/{entityType}/{entityId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string entityType, Guid entityId, CancellationToken ct)
    {
        var result = await _wishlist.ToggleAsync(entityType, entityId, ct);

        if (result.RequireSignOut)
            return Unauthorized(new { error = "Please sign in to save favorites." });

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode == 0 ? 500 : result.StatusCode,
                new { error = result.Error ?? "Could not update your wishlist." });

        return Ok(new { isFavorited = result.Data });
    }

    [HttpPost("accounts/wishlist/remove-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAll(CancellationToken ct)
    {
        var result = await _wishlist.RemoveAllAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error ?? _localizer["Accounts.Wishlist.RemoveFailed"].Value });

            return await WishlistCardPartialAsync(ct);
        }

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.WishlistCleared"]);
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ──

    /// <summary>Re-fetches the wishlist and returns the swappable card partial for
    /// AJAX callers (RemoveAll). Falls back to BadRequest on a fetch failure so the
    /// client surfaces an error toast instead of a broken swap.</summary>
    private async Task<IActionResult> WishlistCardPartialAsync(CancellationToken ct)
    {
        var refreshed = await _wishlist.GetWishlistAsync(ct);
        if (GuardSignOut(refreshed) is { } signOut) return signOut;

        return PartialView("_WishlistCard", refreshed.Data ?? new WishlistVm());
    }

    private async Task PopulateSidebarAsync(CancellationToken ct)
    {
        var profile = await _profile.GetAsync(ct);
        if (profile is { IsSuccess: true, Data: { } p })
        {
            ViewBag.Sidebar = new AccountSidebarVm
            {
                AvatarUrl = p.AvatarUrl,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName)
                    ? $"{p.FirstName} {p.LastName}".Trim()
                    : p.DisplayName,
                Email = p.Email,
            };
        }
        else
        {
            ViewBag.Sidebar = new AccountSidebarVm();
        }
    }
}
