using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Reviews;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Areas.Public.Models.AccessibilityReviews;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// Reviews hub (Phase 3 of the Accounts master plan): "My reviews" and "Accessibility"
/// tabs on one page. The old /accounts/accessibility-reviews page permanently redirects
/// here; its Delete POST stays on the AccessibilityReviews controller.
/// </summary>
[Area("Accounts")]
[Authorize]
[RequirePermission(WebPermission.Review.Read)]
public sealed class ReviewsController : BaseController
{
    private readonly ReviewsFacade _reviews;
    private readonly Areas.Public.Facades.AccessibilityReviewsFacade _accessibilityReviews;
    private readonly ProfileFacade _profile;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ReviewsController(
        ReviewsFacade reviews,
        Areas.Public.Facades.AccessibilityReviewsFacade accessibilityReviews,
        ProfileFacade profile,
        IStringLocalizer<SharedResource> localizer)
    {
        _reviews = reviews;
        _accessibilityReviews = accessibilityReviews;
        _profile = profile;
        _localizer = localizer;
    }

    [HttpGet("accounts/reviews")]
    public async Task<IActionResult> Index(string? tab, CancellationToken ct = default)
    {
        ViewData["AccountNav"] = "Reviews";
        await PopulateSidebarAsync(ct);

        // Compose both tabs in parallel (UI-PERF-API1).
        var reviewsTask = _reviews.GetAsync(ct);
        var accessibilityTask = _accessibilityReviews.GetMyAsync(ct);
        await Task.WhenAll(reviewsTask, accessibilityTask);

        var reviewsResult = reviewsTask.Result;
        if (GuardSignOut(reviewsResult) is { } signOut) return signOut;

        if (!reviewsResult.IsSuccess || reviewsResult.Data is null)
        {
            SetError(reviewsResult.Error);
        }

        // Accessibility tab is best-effort: API stays authoritative on permissions and a
        // failure there must not break the My reviews tab (UI-ERR-ERR3).
        var accessibilityResult = accessibilityTask.Result;

        var vm = new ReviewsHubVm
        {
            Reviews = reviewsResult.Data ?? new ReviewsVm(),
            Accessibility = accessibilityResult is { IsSuccess: true, Data: { } acc }
                ? acc
                : new MyAccessibilityReviewsVm(),
            ActiveTab = string.Equals(tab, "accessibility", StringComparison.OrdinalIgnoreCase)
                ? "accessibility"
                : "mine",
        };

        return View(vm);
    }

    [HttpPost("accounts/reviews/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Review.Update)]
    public async Task<IActionResult> Edit(EditReviewFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            if (WantsAjax())
                return BadRequest(new { error = _localizer["Accounts.Msg.FormError"].Value });

            SetError(_localizer["Accounts.Msg.FormError"]);
            return RedirectToAction(nameof(Index));
        }

        var result = await _reviews.EditAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error ?? _localizer["Accounts.Msg.ReviewUpdateFailed"].Value });

            return await MyReviewsPartialAsync(ct);
        }

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.ReviewUpdated"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.ReviewUpdateFailed"].Value);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("accounts/reviews/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Review.Delete)]
    public async Task<IActionResult> Delete(Guid id, string? rowVersion, CancellationToken ct)
    {
        var result = await _reviews.DeleteAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error ?? _localizer["Accounts.Msg.ReviewDeleteFailed"].Value });

            return await MyReviewsPartialAsync(ct);
        }

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.ReviewDeleted"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.ReviewDeleteFailed"].Value);

        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ──

    /// <summary>
    /// Re-fetches the caller's reviews and returns the "My reviews" tab fragment for an
    /// AJAX swap. Mirrors the PRG path's data source so JS and no-JS render identically.
    /// </summary>
    private async Task<IActionResult> MyReviewsPartialAsync(CancellationToken ct)
    {
        var result = await _reviews.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        return PartialView("_MyReviewsTab", result.Data ?? new ReviewsVm());
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
