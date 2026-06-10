using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Caching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.AccessibilityReviews;
using YallaJo.Web.Areas.Public.Models.Reviews;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
public sealed class ToursController : BaseController
{
    private const string TargetType = "Tour";

    private readonly ToursFacade _tours;
    private readonly ReviewsFacade _reviews;
    private readonly AccessibilityReviewsFacade _accessibilityReviews;

    public ToursController(ToursFacade tours, ReviewsFacade reviews, AccessibilityReviewsFacade accessibilityReviews)
    {
        _tours = tours;
        _reviews = reviews;
        _accessibilityReviews = accessibilityReviews;
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

    // ── Accessibility reviews (login-gated create/edit/delete) ──────────────────

    [HttpPost("tours/{slug}/accessibility-reviews")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccessibilityReview(
        string slug, [Bind(Prefix = "AccessibilityReview")] AccessibilityReviewFormVm form, CancellationToken ct = default)
    {
        form.TargetType = TargetType;
        if (!ModelState.IsValid)
        {
            SetError("Please complete the accessibility review form, including at least one feature.");
            return RedirectToAction(nameof(Detail), new { slug });
        }

        var result = await _accessibilityReviews.SubmitAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Thanks for your accessibility review!");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("tours/{slug}/accessibility-reviews/{reviewId:guid}/edit")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAccessibilityReview(
        string slug, Guid reviewId, [Bind(Prefix = "AccessibilityReview")] AccessibilityReviewFormVm form, CancellationToken ct = default)
    {
        form.TargetType = TargetType;
        if (!ModelState.IsValid)
        {
            SetError("Please complete the accessibility review form, including at least one feature.");
            return RedirectToAction(nameof(Detail), new { slug });
        }

        var result = await _accessibilityReviews.EditAsync(reviewId, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your accessibility review was updated.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("tours/{slug}/accessibility-reviews/{reviewId:guid}/delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccessibilityReview(string slug, Guid reviewId, CancellationToken ct = default)
    {
        var result = await _accessibilityReviews.DeleteAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your accessibility review was deleted.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("tours/{slug}/reviews")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReview(string slug, [Bind(Prefix = "Review")] ReviewFormVm form, CancellationToken ct = default)
    {
        form.TargetType = TargetType;

        if (!ModelState.IsValid)
        {
            SetError("Please complete the review form.");
            return RedirectToAction(nameof(Detail), new { slug });
        }

        var result = await _reviews.SubmitReviewAsync(form, ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Thanks for your review!");
        else if (!ApplyValidationErrors(result))
            SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("tours/{slug}/reviews/{reviewId:guid}/edit")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReview(string slug, Guid reviewId, [Bind(Prefix = "Review")] ReviewEditFormVm form, CancellationToken ct = default)
    {
        form.ReviewId = reviewId;
        if (!ModelState.IsValid)
        {
            SetError("Please complete the review form.");
            return RedirectToAction(nameof(Detail), new { slug });
        }

        var result = await _reviews.EditReviewAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your review was updated.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("tours/{slug}/reviews/{reviewId:guid}/delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(string slug, Guid reviewId, string? rowVersion, CancellationToken ct = default)
    {
        var result = await _reviews.DeleteReviewAsync(reviewId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your review was deleted.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("tours/{slug}/reviews/{reviewId:guid}/helpful")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkHelpful(string slug, Guid reviewId, CancellationToken ct = default)
    {
        var result = await _reviews.MarkHelpfulAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Marked as helpful.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("tours/{slug}/reviews/{reviewId:guid}/unhelpful")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnmarkHelpful(string slug, Guid reviewId, CancellationToken ct = default)
    {
        var result = await _reviews.UnmarkHelpfulAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Helpful vote removed.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("tours/{slug}/reviews/{reviewId:guid}/report")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportReview(string slug, Guid reviewId, [Bind(Prefix = "Report")] ReportFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please choose a reason and add a short description.");
            return RedirectToAction(nameof(Detail), new { slug });
        }

        var result = await _reviews.ReportReviewAsync(reviewId, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Thanks for reporting. Our team will review it.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("tours/{slug}/report")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(string slug, [Bind(Prefix = "Report")] ReportFormVm form, CancellationToken ct = default)
    {
        form.EntityType = TargetType;

        if (!ModelState.IsValid)
        {
            SetError("Please choose a reason and add a short description.");
            return RedirectToAction(nameof(Detail), new { slug });
        }

        var result = await _reviews.SubmitReportAsync(form, ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Thanks for reporting. Our team will review it.");
        else if (!ApplyValidationErrors(result))
            SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

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
            SetSuccess("Your request to join has been sent.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }
}
