using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.AccessibilityReviews;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// "My Accessibility Reviews" — the caller's own accessibility reviews. Edits happen on
/// the entity detail page (within the 48h window); this page lists them and allows delete.
/// </summary>
[Area("Accounts")]
[Authorize]
[RequirePermission(WebPermission.AccessibilityReview.Read)]
public sealed class AccessibilityReviewsController : BaseController
{
    private readonly AccessibilityReviewsFacade _reviews;
    private readonly ProfileFacade _profile;

    public AccessibilityReviewsController(AccessibilityReviewsFacade reviews, ProfileFacade profile)
    {
        _reviews = reviews;
        _profile = profile;
    }

    [HttpGet("accounts/accessibility-reviews")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AccountNav"] = "AccessibilityReviews";
        await PopulateSidebarAsync(ct);

        var result = await _reviews.GetMyAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new MyAccessibilityReviewsVm());
        }

        return View(result.Data);
    }

    [HttpPost("accounts/accessibility-reviews/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AccessibilityReview.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _reviews.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess) SetSuccess("Your accessibility review has been deleted.");
        else SetError(result.Error ?? "Could not delete the accessibility review.");

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSidebarAsync(CancellationToken ct)
    {
        var profile = await _profile.GetAsync(ct);
        ViewBag.Sidebar = profile is { IsSuccess: true, Data: { } p }
            ? new AccountSidebarVm
            {
                AvatarUrl = p.AvatarUrl,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName)
                    ? $"{p.FirstName} {p.LastName}".Trim()
                    : p.DisplayName,
                Email = p.Email,
            }
            : new AccountSidebarVm();
    }
}
