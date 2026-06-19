using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.AccessibilityReviews;
using YallaJo.Web.Areas.Public.Models.Reviews;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Public.Controllers;

/// <summary>
/// Unified review surface for all public review targets (Phase 5, master plan §5).
/// Replaces the ten near-identical review/accessibility-review/report actions that
/// were previously duplicated across ToursController, PlacesController,
/// DirectoryController and GuidesController (~40 actions collapsed into 11).
/// Routes are keyed by a lowercase target token (tour|place|business|guide) which
/// maps to the backend TargetType discriminator (note: guide → "TourGuide").
/// Every form posts a hidden returnUrl; PRG redirects back to the detail page
/// (UI-UX-PE1). When the request is AJAX (WantsAjax) the action returns a
/// refreshed partial or JSON instead, enabling in-place swaps (UI-UX-NF6).
/// </summary>
[Area("Public")]
public sealed class ReviewsController : BaseController
{
    /// <summary>Route token constraint: only these four public target kinds exist.</summary>
    private const string TargetTypeToken = "{targetType:regex(^(tour|place|business|guide)$)}";

    private readonly ReviewsFacade _reviews;
    private readonly AccessibilityReviewsFacade _accessibilityReviews;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ReviewsController(
        ReviewsFacade reviews,
        AccessibilityReviewsFacade accessibilityReviews,
        IStringLocalizer<SharedResource> localizer)
    {
        _reviews = reviews;
        _accessibilityReviews = accessibilityReviews;
        _localizer = localizer;
    }

    /// <summary>Maps the lowercase route token to the backend TargetType discriminator.</summary>
    private static string ToTargetType(string token) => token.ToLowerInvariant() switch
    {
        "tour" => "Tour",
        "place" => "Place",
        "business" => "Business",
        "guide" => "TourGuide",
        _ => "Place",
    };

    // ── Reviews ────────────────────────────────────────────────────────────────

    [HttpPost("reviews/" + TargetTypeToken + "/{targetId:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReview(
        string targetType,
        Guid targetId,
        [Bind(Prefix = "Review")] ReviewFormVm form,
        string? returnUrl,
        List<IFormFile>? reviewImages,
        CancellationToken ct)
    {
        form.TargetType = ToTargetType(targetType);
        form.TargetId = targetId;

        if (!ModelState.IsValid)
        {
            return await FailAsync(_localizer["Public.Review.FormError"], targetType, targetId, returnUrl, ct);
        }

        var submission = await _reviews.SubmitReviewWithImagesAsync(form, reviewImages ?? [], ct);
        if (GuardSignOut(submission.Result) is { } signOut)
        {
            return signOut;
        }

        if (submission.IsSuccess)
        {
            // The review is persisted even if some image uploads failed (no rollback);
            // surface a non-blocking warning appended to the success message in that case.
            var message = submission.AnyImageFailed
                ? $"{_localizer["Public.Review.Created"].Value} {_localizer["Public.Review.ImagesPartialFailure"].Value}"
                : _localizer["Public.Review.Created"].Value;
            return await SucceedAsync(message, targetType, targetId, returnUrl, ct);
        }

        if (!ApplyValidationErrors(submission.Result))
        {
            SetError(submission.Result.Error);
        }

        return await FailAsync(null, targetType, targetId, returnUrl, ct);
    }

    [HttpPost("reviews/" + TargetTypeToken + "/{targetId:guid}/{reviewId:guid}/edit")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReview(
        string targetType,
        Guid targetId,
        Guid reviewId,
        [Bind(Prefix = "Review")] ReviewEditFormVm form,
        string? returnUrl,
        CancellationToken ct)
    {
        form.ReviewId = reviewId;

        if (!ModelState.IsValid)
        {
            return await FailAsync(_localizer["Public.Review.FormError"], targetType, targetId, returnUrl, ct);
        }

        var result = await _reviews.EditReviewAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            return await SucceedAsync(_localizer["Public.Review.Updated"], targetType, targetId, returnUrl, ct);
        }

        if (!ApplyValidationErrors(result))
        {
            SetError(result.Error);
        }

        return await FailAsync(null, targetType, targetId, returnUrl, ct);
    }

    [HttpPost("reviews/" + TargetTypeToken + "/{targetId:guid}/{reviewId:guid}/delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(
        string targetType,
        Guid targetId,
        Guid reviewId,
        string? rowVersion,
        string? returnUrl,
        CancellationToken ct)
    {
        var result = await _reviews.DeleteReviewAsync(reviewId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            return await SucceedAsync(_localizer["Public.Review.Deleted"], targetType, targetId, returnUrl, ct);
        }

        SetError(result.Error);
        return await FailAsync(null, targetType, targetId, returnUrl, ct);
    }

    [HttpPost("reviews/" + TargetTypeToken + "/{targetId:guid}/{reviewId:guid}/helpful")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkHelpful(
        string targetType,
        Guid targetId,
        Guid reviewId,
        string? returnUrl,
        CancellationToken ct)
    {
        var result = await _reviews.MarkHelpfulAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        // Optimistic-UI friendly JSON for AJAX callers (UI-UX-NF6).
        if (WantsAjax())
        {
            return Json(new { success = result.IsSuccess, error = result.IsSuccess ? null : result.Error });
        }

        if (result.IsSuccess)
        {
            SetSuccess(_localizer["Public.Review.Helpful"]);
        }
        else
        {
            SetError(result.Error);
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost("reviews/" + TargetTypeToken + "/{targetId:guid}/{reviewId:guid}/unhelpful")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnmarkHelpful(
        string targetType,
        Guid targetId,
        Guid reviewId,
        string? returnUrl,
        CancellationToken ct)
    {
        var result = await _reviews.UnmarkHelpfulAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (WantsAjax())
        {
            return Json(new { success = result.IsSuccess, error = result.IsSuccess ? null : result.Error });
        }

        if (result.IsSuccess)
        {
            SetSuccess(_localizer["Public.Review.HelpfulRemoved"]);
        }
        else
        {
            SetError(result.Error);
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost("reviews/" + TargetTypeToken + "/{targetId:guid}/{reviewId:guid}/report")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportReview(
        string targetType,
        Guid targetId,
        Guid reviewId,
        [Bind(Prefix = "Report")] ReportFormVm form,
        string? returnUrl,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return await FailAsync(_localizer["Public.Report.FormError"], targetType, targetId, returnUrl, ct);
        }

        var result = await _reviews.ReportReviewAsync(reviewId, form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            return await SucceedAsync(_localizer["Public.Review.Reported"], targetType, targetId, returnUrl, ct);
        }

        if (!ApplyValidationErrors(result))
        {
            SetError(result.Error);
        }

        return await FailAsync(null, targetType, targetId, returnUrl, ct);
    }

    /// <summary>Report the reviewed entity itself (tour/place/business/guide), not a review.</summary>
    [HttpPost("reviews/" + TargetTypeToken + "/{targetId:guid}/entity-report")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportEntity(
        string targetType,
        Guid targetId,
        [Bind(Prefix = "Report")] ReportFormVm form,
        string? returnUrl,
        CancellationToken ct)
    {
        form.EntityType = ToTargetType(targetType);
        form.EntityId = targetId;

        if (!ModelState.IsValid)
        {
            return await FailAsync(_localizer["Public.Report.FormError"], targetType, targetId, returnUrl, ct);
        }

        var result = await _reviews.SubmitReportAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            return await SucceedAsync(_localizer["Public.Review.Reported"], targetType, targetId, returnUrl, ct);
        }

        if (!ApplyValidationErrors(result))
        {
            SetError(result.Error);
        }

        return await FailAsync(null, targetType, targetId, returnUrl, ct);
    }

    // ── Accessibility reviews ──────────────────────────────────────────────────

    [HttpPost("reviews/" + TargetTypeToken + "/{targetId:guid}/accessibility")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccessibilityReview(
        string targetType,
        Guid targetId,
        [Bind(Prefix = "AccessibilityReview")] AccessibilityReviewFormVm form,
        string? returnUrl,
        CancellationToken ct)
    {
        form.TargetType = ToTargetType(targetType);
        form.TargetId = targetId;

        if (!ModelState.IsValid)
        {
            return await FailAccessibilityAsync(_localizer["Public.AccessibilityReview.FormError"], targetType, targetId, returnUrl, ct);
        }

        var result = await _accessibilityReviews.SubmitAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            return await SucceedAccessibilityAsync(_localizer["Public.AccessibilityReview.Created"], targetType, targetId, returnUrl, ct);
        }

        if (!ApplyValidationErrors(result))
        {
            SetError(result.Error);
        }

        return await FailAccessibilityAsync(null, targetType, targetId, returnUrl, ct);
    }

    [HttpPost("reviews/" + TargetTypeToken + "/{targetId:guid}/accessibility/{reviewId:guid}/edit")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAccessibilityReview(
        string targetType,
        Guid targetId,
        Guid reviewId,
        [Bind(Prefix = "AccessibilityReview")] AccessibilityReviewFormVm form,
        string? returnUrl,
        CancellationToken ct)
    {
        form.TargetType = ToTargetType(targetType);
        form.TargetId = targetId;

        if (!ModelState.IsValid)
        {
            return await FailAccessibilityAsync(_localizer["Public.AccessibilityReview.FormError"], targetType, targetId, returnUrl, ct);
        }

        var result = await _accessibilityReviews.EditAsync(reviewId, form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            return await SucceedAccessibilityAsync(_localizer["Public.AccessibilityReview.Updated"], targetType, targetId, returnUrl, ct);
        }

        if (!ApplyValidationErrors(result))
        {
            SetError(result.Error);
        }

        return await FailAccessibilityAsync(null, targetType, targetId, returnUrl, ct);
    }

    [HttpPost("reviews/" + TargetTypeToken + "/{targetId:guid}/accessibility/{reviewId:guid}/delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccessibilityReview(
        string targetType,
        Guid targetId,
        Guid reviewId,
        string? returnUrl,
        CancellationToken ct)
    {
        var result = await _accessibilityReviews.DeleteAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (result.IsSuccess)
        {
            return await SucceedAccessibilityAsync(_localizer["Public.AccessibilityReview.Deleted"], targetType, targetId, returnUrl, ct);
        }

        SetError(result.Error);
        return await FailAccessibilityAsync(null, targetType, targetId, returnUrl, ct);
    }

    // ── Read (AJAX refresh / pagination) ───────────────────────────────────────

    /// <summary>Returns the reviews list partial for in-place refresh and paging.</summary>
    [HttpGet("reviews/" + TargetTypeToken + "/{targetId:guid}/list")]
    [AllowAnonymous]
    public async Task<IActionResult> List(string targetType, Guid targetId, int page = 1, CancellationToken ct = default)
    {
        var list = await _reviews.GetReviewListAsync(ToTargetType(targetType), targetId, Math.Max(1, page), ct);
        return PartialView("_Reviews", list);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>PRG success: flash + redirect back, or refreshed partial for AJAX callers.</summary>
    private async Task<IActionResult> SucceedAsync(string message, string targetType, Guid targetId, string? returnUrl, CancellationToken ct)
    {
        if (WantsAjax())
        {
            var list = await _reviews.GetReviewListAsync(ToTargetType(targetType), targetId, 1, ct);
            return PartialView("_Reviews", list);
        }

        SetSuccess(message);
        return RedirectBack(returnUrl);
    }

    private async Task<IActionResult> FailAsync(string? message, string targetType, Guid targetId, string? returnUrl, CancellationToken ct)
    {
        if (message is not null)
        {
            SetError(message);
        }

        if (WantsAjax())
        {
            // AJAX callers surface the error as a toast from the JSON body (UI-UX-NF2).
            var error = message ?? _localizer["Public.Results.Error"].Value;
            return BadRequest(new { error });
        }

        await Task.CompletedTask;
        return RedirectBack(returnUrl);
    }

    private async Task<IActionResult> SucceedAccessibilityAsync(string message, string targetType, Guid targetId, string? returnUrl, CancellationToken ct)
    {
        if (WantsAjax())
        {
            var list = await _accessibilityReviews.GetListAsync(ToTargetType(targetType), targetId, 1, ct);
            return PartialView("_AccessibilityReviews", list);
        }

        SetSuccess(message);
        return RedirectBack(returnUrl);
    }

    private async Task<IActionResult> FailAccessibilityAsync(string? message, string targetType, Guid targetId, string? returnUrl, CancellationToken ct)
    {
        if (message is not null)
        {
            SetError(message);
        }

        if (WantsAjax())
        {
            var error = message ?? _localizer["Public.Results.Error"].Value;
            return BadRequest(new { error });
        }

        await Task.CompletedTask;
        return RedirectBack(returnUrl);
    }

    /// <summary>Validated local redirect (open-redirect safe); falls back to the public home page.</summary>
    private IActionResult RedirectBack(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home", new { area = "Public" });
}
