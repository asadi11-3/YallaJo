using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Wishlist;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class WishlistController : BaseController
{
    private readonly WishlistFacade _wishlist;
    private readonly ProfileFacade _profile;

    public WishlistController(WishlistFacade wishlist, ProfileFacade profile)
    {
        _wishlist = wishlist;
        _profile = profile;
    }

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
            SetSuccess("Removed from your wishlist.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("accounts/wishlist/remove-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAll(CancellationToken ct)
    {
        var result = await _wishlist.RemoveAllAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your wishlist has been cleared.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
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
