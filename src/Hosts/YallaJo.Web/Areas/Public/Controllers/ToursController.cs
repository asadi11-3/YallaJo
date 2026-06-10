using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Public.Caching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
public sealed class ToursController : BaseController
{
    private const string TargetType = "Tour";

    private readonly ToursFacade _tours;
    private readonly ReviewsFacade _reviews;
    private readonly AccessibilityReviewsFacade _accessibilityReviews;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ToursController(ToursFacade tours, ReviewsFacade reviews, AccessibilityReviewsFacade accessibilityReviews, IStringLocalizer<SharedResource> localizer)
    {
        _tours = tours;
        _reviews = reviews;
        _accessibilityReviews = accessibilityReviews;
        _localizer = localizer;
    }

    // Phase 4: /tours is the canonical search surface. Filter params map 1:1 to the
    // backend GET /api/v1/tours/search contract (UI-UX-S1/S3). The output cache must
    // vary by X-Requested-With because the same URL serves both the full page and the
    // _TourResults partial for AJAX refinement (api-client.js always sends the header).
    [HttpGet("tours")]
    [OutputCache(PolicyName = "PublicShort", VaryByHeaderNames = new[] { "X-Requested-With" })]
    public async Task<IActionResult> Index(
        int page = 1,
        string? sort = null,
        string? q = null,
        Guid? placeId = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        string? difficulty = null,
        int? durationMin = null,
        int? durationMax = null,
        bool? childFriendly = null,
        bool? accessible = null,
        bool? instantBooking = null,
        bool? hasDiscount = null,
        decimal? minRating = null,
        CancellationToken ct = default)
    {
        var filters = new TourFilterVm
        {
            PriceMin = priceMin is < 0 ? null : priceMin,
            PriceMax = priceMax is < 0 ? null : priceMax,
            Difficulty = NormalizeDifficulty(difficulty),
            DurationMin = durationMin is < 0 ? null : durationMin,
            DurationMax = durationMax is < 0 ? null : durationMax,
            ChildFriendly = childFriendly,
            Accessible = accessible,
            InstantBooking = instantBooking,
            HasDiscount = hasDiscount,
            MinRating = minRating is < 0 or > 5 ? null : minRating
        };

        var result = await _tours.GetGridAsync(page, sort, q, placeId, filters, ct);

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            var fallback = new TourGridVm { Sort = ToursFacade.NormalizeSort(sort), Query = q, PlaceId = placeId, Filters = filters };
            return WantsAjax() ? PartialView("_TourResults", fallback) : View(fallback);
        }

        return WantsAjax() ? PartialView("_TourResults", result.Data) : View(result.Data);
    }

    /// <summary>Whitelist difficulty tokens (search index stores lowercase names).</summary>
    private static string? NormalizeDifficulty(string? difficulty)
    {
        if (string.IsNullOrWhiteSpace(difficulty)) return null;
        var token = difficulty.Trim().ToLowerInvariant();
        return token is "easy" or "moderate" or "hard" or "expert" ? token : null;
    }

    [HttpGet("tours/{slug}")]
    [OutputCache(PolicyName = "PublicMedium")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct = default)
    {
        var result = await _tours.GetDetailAsync(slug, ct);

        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
                return NotFound();

            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        PublicOutputCacheTagger.AddTag(HttpContext, $"tour:{result.Data.Id}");
        ViewData["Reviews"] = await _reviews.GetReviewListAsync(TargetType, result.Data.Id, 1, ct);
        ViewData["AccessibilityReviews"] = await _accessibilityReviews.GetListAsync(TargetType, result.Data.Id, 1, ct);
        return View(result.Data);
    }

    // Review/report/accessibility-review actions moved to the unified ReviewsController (Phase 5).

    [HttpPost("tours/{slug}/join")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestJoin(string slug, Guid tourBookingId, Guid availabilitySlotId, int participantCount, string? message, CancellationToken ct = default)
    {
        var result = await _tours.SubmitJoinRequestAsync(
            new SubmitJoinRequestBody(tourBookingId, availabilitySlotId, participantCount, message), ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess(_localizer["Public.JoinRequest.Sent"]);
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }
}
