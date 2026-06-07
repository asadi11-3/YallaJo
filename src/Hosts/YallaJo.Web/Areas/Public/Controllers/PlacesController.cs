using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Caching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Places;
using YallaJo.Web.Areas.Public.Models.Reviews;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
public sealed class PlacesController : BaseController
{
    private const string TargetType = "Place";

    private readonly PlacesFacade _places;
    private readonly ReviewsFacade _reviews;

    public PlacesController(PlacesFacade places, ReviewsFacade reviews)
    {
        _places = places;
        _reviews = reviews;
    }

    // ── GET /places ───────────────────────────────────────────────────────────
    [HttpGet("places")]
    [OutputCache(PolicyName = "PublicShort")]
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
            return View(new PlacesGridVm { Filters = filters });
        }

        return View(result.Data);
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
        return View(result.Data);
    }

    // ── POST /places/{slug}/reviews (login-gated review create) ─────────────────
    [HttpPost("places/{slug}/reviews")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReview(string slug, [Bind(Prefix = "Review")] ReviewFormVm form, CancellationToken ct = default)
    {
        form.TargetType = TargetType;
        if (!ModelState.IsValid)
        {
            SetError("Please complete the review form.");
            return RedirectToAction(nameof(Details), new { slug });
        }

        var result = await _reviews.SubmitReviewAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Thanks for your review!");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpPost("places/{slug}/reviews/{reviewId:guid}/edit")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReview(string slug, Guid reviewId, [Bind(Prefix = "Review")] ReviewEditFormVm form, CancellationToken ct = default)
    {
        form.ReviewId = reviewId;
        if (!ModelState.IsValid)
        {
            SetError("Please complete the review form.");
            return RedirectToAction(nameof(Details), new { slug });
        }

        var result = await _reviews.EditReviewAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your review was updated.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpPost("places/{slug}/reviews/{reviewId:guid}/delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(string slug, Guid reviewId, string? rowVersion, CancellationToken ct = default)
    {
        var result = await _reviews.DeleteReviewAsync(reviewId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your review was deleted.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpPost("places/{slug}/reviews/{reviewId:guid}/helpful")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkHelpful(string slug, Guid reviewId, CancellationToken ct = default)
    {
        var result = await _reviews.MarkHelpfulAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Marked as helpful.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpPost("places/{slug}/reviews/{reviewId:guid}/unhelpful")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnmarkHelpful(string slug, Guid reviewId, CancellationToken ct = default)
    {
        var result = await _reviews.UnmarkHelpfulAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Helpful vote removed.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpPost("places/{slug}/reviews/{reviewId:guid}/report")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportReview(string slug, Guid reviewId, [Bind(Prefix = "Report")] ReportFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please choose a reason and add a short description.");
            return RedirectToAction(nameof(Details), new { slug });
        }

        var result = await _reviews.ReportReviewAsync(reviewId, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Thanks for reporting. Our team will review it.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Details), new { slug });
    }

    // ── POST /places/{slug}/report (login-gated content report) ─────────────────
    [HttpPost("places/{slug}/report")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(string slug, [Bind(Prefix = "Report")] ReportFormVm form, CancellationToken ct = default)
    {
        form.EntityType = TargetType;
        if (!ModelState.IsValid)
        {
            SetError("Please choose a reason and add a short description.");
            return RedirectToAction(nameof(Details), new { slug });
        }

        var result = await _reviews.SubmitReportAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Thanks for reporting. Our team will review it.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Details), new { slug });
    }

    [HttpPost("places/{slug}/recommendations/sponsored-click")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SponsoredClick(string slug, SponsoredClickFormVm form, CancellationToken ct = default)
    {
        var result = await _places.RecordSponsoredClickAsync(form, ct);
        if (!result.IsSuccess && !ApplyValidationErrors(result)) SetError(result.Error);
        return RedirectToAction(nameof(Details), new { slug });
    }
}
