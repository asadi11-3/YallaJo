using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Caching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Places;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
public sealed class PlacesController : BaseController
{
    private const string TargetType = "Place";

    private readonly PlacesFacade _places;
    private readonly ReviewsFacade _reviews;
    private readonly AccessibilityReviewsFacade _accessibilityReviews;

    public PlacesController(PlacesFacade places, ReviewsFacade reviews, AccessibilityReviewsFacade accessibilityReviews)
    {
        _places = places;
        _reviews = reviews;
        _accessibilityReviews = accessibilityReviews;
    }

    // ── GET /places ───────────────────────────────────────────────────────────
    [HttpGet("places")]
    [OutputCache(PolicyName = "PublicShort", VaryByHeaderNames = new[] { "X-Requested-With" })]
    public async Task<IActionResult> Index(
        string? city = null,
        string? country = null,
        int? ratingMin = null,
        bool hasActiveTours = false,
        int page = 1,
        CancellationToken ct = default)
    {
        var filters = new PlaceFiltersVm
        {
            City           = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Country        = string.IsNullOrWhiteSpace(country) ? null : country.Trim(),
            RatingMin      = ratingMin,
            HasActiveTours = hasActiveTours,
        };

        var result = await _places.GetGridAsync(filters, page, ct);

        if (!result.IsSuccess || result.Data is null)
        {
            // Tolerant: render a friendly empty grid (with the echoed filters
            // preserved) plus the error message, rather than a 500.
            SetError(result.Error);
            var fallback = new PlacesGridVm { Filters = filters };
            return WantsAjax() ? PartialView("_PlacesResults", fallback) : View(fallback);
        }

        // Phase 7: AJAX requests (listing.js) receive just the results fragment.
        return WantsAjax() ? PartialView("_PlacesResults", result.Data) : View(result.Data);
    }

    // ── GET /places/{slug} ──────────────────────────────────────────────────────
    [HttpGet("places/{slug}")]
    [OutputCache(PolicyName = "PublicMedium")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct = default)
    {
        var result = await _places.GetDetailAsync(slug, ct);

        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
                return NotFound();

            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        PublicOutputCacheTagger.AddTag(HttpContext, $"place:{result.Data.PlaceId}");
        ViewData["Reviews"] = await _reviews.GetReviewListAsync(TargetType, result.Data.PlaceId, 1, ct);
        ViewData["AccessibilityReviews"] = await _accessibilityReviews.GetListAsync(TargetType, result.Data.PlaceId, 1, ct);
        return View(result.Data);
    }

    // Review/report/accessibility-review actions moved to the unified ReviewsController (Phase 5).

    [HttpPost("places/{slug}/recommendations/sponsored-click")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SponsoredClick(string slug, SponsoredClickFormVm form, CancellationToken ct = default)
    {
        var result = await _places.RecordSponsoredClickAsync(form, ct);
        if (!result.IsSuccess && !ApplyValidationErrors(result)) SetError(result.Error);
        return RedirectToAction(nameof(Details), new { slug });
    }
}
