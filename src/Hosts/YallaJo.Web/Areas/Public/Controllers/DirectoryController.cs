using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Caching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Directory;
using YallaJo.Web.Areas.Public.Models.Reviews;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class DirectoryController : BaseController
{
    private const string TargetType = "Business";

    private readonly DirectoryFacade _directory;
    private readonly ReviewsFacade _reviews;

    public DirectoryController(DirectoryFacade directory, ReviewsFacade reviews)
    {
        _directory = directory;
        _reviews = reviews;
    }

    [HttpGet("businesses")]
    [OutputCache(PolicyName = "PublicShort")]
    public async Task<IActionResult> Index(
        string? q = null, string? businessType = null, string? city = null, int page = 1, CancellationToken ct = default)
    {
        var result = await _directory.GetDirectoryAsync(q, businessType, city, page, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new DirectoryVm
            {
                BusinessTypes = DirectoryFacade.BusinessTypeOptions,
                Query = q,
                SelectedBusinessType = DirectoryFacade.NormalizeBusinessType(businessType),
                City = city,
            });
        }

        return View(result.Data);
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
        return View(result.Data);
    }

    [HttpPost("businesses/{id:guid}/reviews")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReview(Guid id, [Bind(Prefix = "Review")] ReviewFormVm form, CancellationToken ct = default)
    {
        form.TargetType = TargetType;
        if (!ModelState.IsValid)
        {
            SetError("Please complete the review form.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await _reviews.SubmitReviewAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Thanks for your review!");
        else if (!ApplyValidationErrors(result))
            SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("businesses/{id:guid}/reviews/{reviewId:guid}/edit")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReview(Guid id, Guid reviewId, [Bind(Prefix = "Review")] ReviewEditFormVm form, CancellationToken ct = default)
    {
        form.ReviewId = reviewId;
        if (!ModelState.IsValid)
        {
            SetError("Please complete the review form.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await _reviews.EditReviewAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your review was updated.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("businesses/{id:guid}/reviews/{reviewId:guid}/delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(Guid id, Guid reviewId, string? rowVersion, CancellationToken ct = default)
    {
        var result = await _reviews.DeleteReviewAsync(reviewId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your review was deleted.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("businesses/{id:guid}/reviews/{reviewId:guid}/helpful")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkHelpful(Guid id, Guid reviewId, CancellationToken ct = default)
    {
        var result = await _reviews.MarkHelpfulAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Marked as helpful.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("businesses/{id:guid}/reviews/{reviewId:guid}/unhelpful")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnmarkHelpful(Guid id, Guid reviewId, CancellationToken ct = default)
    {
        var result = await _reviews.UnmarkHelpfulAsync(reviewId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Helpful vote removed.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("businesses/{id:guid}/reviews/{reviewId:guid}/report")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportReview(Guid id, Guid reviewId, [Bind(Prefix = "Report")] ReportFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please choose a reason and add a short description.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await _reviews.ReportReviewAsync(reviewId, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Thanks for reporting. Our team will review it.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("businesses/{id:guid}/report")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(Guid id, [Bind(Prefix = "Report")] ReportFormVm form, CancellationToken ct = default)
    {
        form.EntityType = TargetType;
        if (!ModelState.IsValid)
        {
            SetError("Please choose a reason and add a short description.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await _reviews.SubmitReportAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Thanks for reporting. Our team will review it.");
        else if (!ApplyValidationErrors(result))
            SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { id });
    }
}
