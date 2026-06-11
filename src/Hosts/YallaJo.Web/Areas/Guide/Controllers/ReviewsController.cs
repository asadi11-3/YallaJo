using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Reviews;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class ReviewsController : GuideBaseController
{
    private const int DefaultPageSize = 20;

    private readonly GuideReviewsFacade _reviews;

    public ReviewsController(GuideReviewsFacade reviews) => _reviews = reviews;

    [HttpGet("guide/reviews")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        SetNav("Reviews");

        var result = await _reviews.GetAsync(page, DefaultPageSize, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        ReviewsVm vm;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            vm = new ReviewsVm();
        }
        else
        {
            vm = result.Data;
        }

        if (WantsAjax())
        {
            return PartialView("_ReviewsResults", vm);
        }

        return View(vm);
    }

    /// <summary>
    /// Posts the guide's reply to a review (B3). AJAX (WantsAjax): 400 + { error } on failure
    /// (provider-actions.js toasts it), otherwise the refreshed swappable reviews fragment —
    /// the client toasts its own data-success-message (NF1). No-JS callers keep the PRG flash.
    /// </summary>
    [HttpPost("guide/reviews/{id:guid}/reply")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ReviewReply.Create)]
    public async Task<IActionResult> Reply(Guid id, string? content, int page = 1, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var result = await _reviews.ReplyAsync(id, content, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Message ?? "Could not post your reply." });
            }

            var refreshed = await _reviews.GetAsync(page, DefaultPageSize, ct);
            if (GuardSignOut(refreshed) is { } refreshSignOut) return refreshSignOut;

            return PartialView("_ReviewsResults", refreshed.Data ?? new ReviewsVm());
        }

        if (!result.IsSuccess)
        {
            SetError(result.Error ?? "Could not post your reply.");
        }
        else
        {
            SetSuccess("Reply posted.");
        }

        return RedirectToAction(nameof(Index), new { page });
    }
}
