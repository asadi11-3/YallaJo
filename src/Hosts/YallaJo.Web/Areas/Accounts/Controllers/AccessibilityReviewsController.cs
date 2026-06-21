using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// "My Accessibility Reviews". Phase 3 (Accounts master plan): the standalone page moved
/// into the Reviews hub (/accounts/reviews?tab=accessibility). Index now permanently
/// redirects there; Delete keeps its route, anti-forgery and permission gate, and PRGs
/// back to the hub tab. Edits still happen on the entity detail page (48h window).
/// </summary>
[Area("Accounts")]
[Authorize]
[RequirePermission(WebPermission.AccessibilityReview.Read)]
public sealed class AccessibilityReviewsController : BaseController
{
    private readonly AccessibilityReviewsFacade _reviews;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AccessibilityReviewsController(AccessibilityReviewsFacade reviews, IStringLocalizer<SharedResource> localizer)
    {
        _reviews = reviews;
        _localizer = localizer;
    }

    [HttpGet("accounts/accessibility-reviews")]
    public IActionResult Index()
        => RedirectPermanent(Url.Action("Index", "Reviews", new { area = "Accounts", tab = "accessibility" })!);

    [HttpPost("accounts/accessibility-reviews/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AccessibilityReview.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _reviews.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error ?? _localizer["Accounts.Msg.AccReviewDeleteFailed"].Value });

            return await AccessibilityTabPartialAsync(ct);
        }

        if (result.IsSuccess) SetSuccess(_localizer["Accounts.Msg.AccReviewDeleted"]);
        else SetError(result.Error ?? _localizer["Accounts.Msg.AccReviewDeleteFailed"].Value);

        return RedirectToAction("Index", "Reviews", new { area = "Accounts", tab = "accessibility" });
    }

    // ── Helpers ──

    /// <summary>
    /// Re-fetches the caller's accessibility reviews and returns the hub's accessibility
    /// tab fragment for an AJAX swap (PRG fallback still redirects to the hub tab).
    /// </summary>
    private async Task<IActionResult> AccessibilityTabPartialAsync(CancellationToken ct)
    {
        var result = await _reviews.GetMyAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        return PartialView(
            "~/Areas/Accounts/Views/Reviews/_AccessibilityReviewsTab.cshtml",
            result.Data ?? new Areas.Public.Models.AccessibilityReviews.MyAccessibilityReviewsVm());
    }
}
