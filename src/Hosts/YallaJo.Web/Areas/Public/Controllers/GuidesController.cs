using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Guides;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class GuidesController(
    GuidesFacade guides,
    ReviewsFacade reviews,
    AccessibilityReviewsFacade accessibilityReviews) : BaseController
{
    private const string TargetType = "TourGuide";

    [HttpGet("guides")]
    [OutputCache(PolicyName = "PublicShort", VaryByHeaderNames = new[] { "X-Requested-With" })]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await guides.GetGridAsync(page, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            var fallback = new GuidesGridVm();
            return WantsAjax() ? PartialView("_GuidesResults", fallback) : View(fallback);
        }

        // Phase 7: AJAX requests (listing.js) receive just the results fragment.
        return WantsAjax() ? PartialView("_GuidesResults", result.Data) : View(result.Data);
    }

    [HttpGet("guides/{slug}")]
    [OutputCache(PolicyName = "PublicMedium")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct = default)
    {
        var result = await guides.GetDetailAsync(slug, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
            {
                return NotFound();
            }

            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        ViewData["Reviews"] = await reviews.GetReviewListAsync(TargetType, result.Data.Id, 1, ct);
        ViewData["AccessibilityReviews"] = await accessibilityReviews.GetListAsync(TargetType, result.Data.Id, 1, ct);
        return View(result.Data);
    }

    // Review/report/accessibility-review actions moved to the unified ReviewsController (Phase 5).
}
