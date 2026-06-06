using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Guides;
using YallaJo.Web.Areas.Public.Models.Reviews;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class GuidesController(GuidesFacade guides, ReviewsFacade reviews) : BaseController
{
    private const string TargetType = "TourGuide";

    [HttpGet("guides")]
    [OutputCache(PolicyName = "PublicShort")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await guides.GetGridAsync(page, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new GuidesGridVm());
        }

        return View(result.Data);
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
        return View(result.Data);
    }

    [HttpPost("guides/{slug}/reviews")]
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

        var result = await reviews.SubmitReviewAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Thanks for your review!");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("guides/{slug}/reviews/{reviewId:guid}/edit")]
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

        var result = await reviews.EditReviewAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your review was updated.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("guides/{slug}/reviews/{reviewId:guid}/delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(string slug, Guid reviewId, string? rowVersion, CancellationToken ct = default)
    {
        var result = await reviews.DeleteReviewAsync(reviewId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your review was deleted.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("guides/{slug}/reviews/{reviewId:guid}/helpful")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkHelpful(string slug, Guid reviewId, CancellationToken ct = default)
    {
        var result = await reviews.MarkHelpfulAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Marked as helpful.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("guides/{slug}/reviews/{reviewId:guid}/unhelpful")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnmarkHelpful(string slug, Guid reviewId, CancellationToken ct = default)
    {
        var result = await reviews.UnmarkHelpfulAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Helpful vote removed.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("guides/{slug}/reviews/{reviewId:guid}/report")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportReview(string slug, Guid reviewId, [Bind(Prefix = "Report")] ReportFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please choose a reason and add a short description.");
            return RedirectToAction(nameof(Detail), new { slug });
        }

        var result = await reviews.ReportReviewAsync(reviewId, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Thanks for reporting. Our team will review it.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }

    [HttpPost("guides/{slug}/report")]
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

        var result = await reviews.SubmitReportAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Thanks for reporting. Our team will review it.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }
}
