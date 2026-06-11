using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Reviews;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class ReviewsController : BaseController
{
    private readonly ReviewsFacade _reviews;

    public ReviewsController(ReviewsFacade reviews) => _reviews = reviews;

    [HttpGet("provider/reviews")]
    public async Task<IActionResult> Index(Guid? tourId, CancellationToken ct = default)
    {
        var result = await _reviews.GetAsync(tourId, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ReviewsVm());
        }

        return View(result.Data);
    }

    [HttpPost("provider/reviews/{id:guid}/reply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(Guid id, string content, Guid tourId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Fail(L["Provider.Flash.ReplyRequired"], tourId);

        var result = await _reviews.ReplyAsync(id, content, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
            return Fail(result.Error, tourId);

        return await SucceedAsync(L["Provider.Flash.ReplyPosted"], tourId, ct);
    }

    [HttpPost("provider/reviews/{id:guid}/reply/{replyId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReply(Guid id, Guid replyId, string content, Guid tourId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Fail(L["Provider.Flash.ReplyRequired"], tourId);

        var result = await _reviews.EditReplyAsync(id, replyId, content, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
            return Fail(result.Error, tourId);

        return await SucceedAsync(L["Provider.Flash.ReplyUpdated"], tourId, ct);
    }

    [HttpPost("provider/reviews/{id:guid}/reply/{replyId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReply(Guid id, Guid replyId, Guid tourId, CancellationToken ct = default)
    {
        var result = await _reviews.DeleteReplyAsync(id, replyId, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
            return Fail(result.Error, tourId);

        return await SucceedAsync(L["Provider.Flash.ReplyDeleted"], tourId, ct);
    }

    [HttpPost("provider/reviews/{id:guid}/report")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(Guid id, string reason, string? description, Guid tourId, CancellationToken ct = default)
    {
        var result = await _reviews.ReportAsync(id, reason, description, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
            return Fail(result.Error, tourId);

        return await SucceedAsync(L["Provider.Flash.ReportSubmitted"], tourId, ct);
    }

    /// <summary>
    /// AJAX requests (JS5/PE1) get a refreshed <c>_ReviewsResults</c> fragment that
    /// provider-actions.js swaps in place (the success toast comes from the form's
    /// data-success-message attribute). Non-AJAX requests keep the original PRG flow.
    /// </summary>
    private async Task<IActionResult> SucceedAsync(string message, Guid tourId, CancellationToken ct)
    {
        if (WantsAjax())
        {
            var refreshed = await _reviews.GetAsync(tourId, ct);
            return PartialView("_ReviewsResults", refreshed.Data ?? new ReviewsVm());
        }

        SetSuccess(message);
        return RedirectToAction(nameof(Index), new { tourId });
    }

    private IActionResult Fail(string? message, Guid tourId)
    {
        if (WantsAjax())
            return BadRequest(new { error = message });

        SetError(message);
        return RedirectToAction(nameof(Index), new { tourId });
    }
}
