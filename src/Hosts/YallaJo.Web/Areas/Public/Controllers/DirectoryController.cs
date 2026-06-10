using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Caching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Directory;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
public sealed class DirectoryController : BaseController
{
    private const string TargetType = "Business";

    private readonly DirectoryFacade _directory;
    private readonly ReviewsFacade _reviews;
    private readonly AccessibilityReviewsFacade _accessibilityReviews;

    public DirectoryController(DirectoryFacade directory, ReviewsFacade reviews, AccessibilityReviewsFacade accessibilityReviews)
    {
        _directory = directory;
        _reviews = reviews;
        _accessibilityReviews = accessibilityReviews;
    }

    [HttpGet("businesses")]
    [OutputCache(PolicyName = "PublicShort", VaryByHeaderNames = new[] { "X-Requested-With" })]
    public async Task<IActionResult> Index(
        string? q = null, string? businessType = null, string? city = null, int page = 1, CancellationToken ct = default)
    {
        var result = await _directory.GetDirectoryAsync(q, businessType, city, page, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            var fallback = new DirectoryVm
            {
                BusinessTypes = DirectoryFacade.BusinessTypeOptions,
                Query = q,
                SelectedBusinessType = DirectoryFacade.NormalizeBusinessType(businessType),
                City = city,
            };
            return WantsAjax() ? PartialView("_DirectoryResults", fallback) : View(fallback);
        }

        // Phase 7: AJAX requests (listing.js) receive just the results fragment.
        return WantsAjax() ? PartialView("_DirectoryResults", result.Data) : View(result.Data);
    }

    [HttpGet("businesses/{id:guid}")]
    [OutputCache(PolicyName = "PublicMedium")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct = default)
    {
        var result = await _directory.GetDetailAsync(id, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
                return NotFound();

            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        PublicOutputCacheTagger.AddTag(HttpContext, $"business:{result.Data.Id}");
        ViewData["Reviews"] = await _reviews.GetReviewListAsync(TargetType, result.Data.Id, 1, ct);
        ViewData["AccessibilityReviews"] = await _accessibilityReviews.GetListAsync(TargetType, result.Data.Id, 1, ct);
        return View(result.Data);
    }

    // Review/report/accessibility-review actions moved to the unified ReviewsController (Phase 5).
}
