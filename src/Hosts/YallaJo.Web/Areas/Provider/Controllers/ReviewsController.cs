using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Reviews;
using YallaJo.Web.Areas.Provider.Shared;
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
        SetSidebar();
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
        {
            SetError("Please enter a reply.");
            return RedirectToAction(nameof(Index), new { tourId });
        }

        var result = await _reviews.ReplyAsync(id, content, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Your reply was posted.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index), new { tourId });
    }

    [HttpPost("provider/reviews/{id:guid}/reply/{replyId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReply(Guid id, Guid replyId, string content, Guid tourId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            SetError("Please enter a reply.");
            return RedirectToAction(nameof(Index), new { tourId });
        }

        var result = await _reviews.EditReplyAsync(id, replyId, content, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Your reply was updated.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index), new { tourId });
    }

    [HttpPost("provider/reviews/{id:guid}/reply/{replyId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReply(Guid id, Guid replyId, Guid tourId, CancellationToken ct = default)
    {
        var result = await _reviews.DeleteReplyAsync(id, replyId, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Your reply was deleted.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index), new { tourId });
    }

    [HttpPost("provider/reviews/{id:guid}/report")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(Guid id, string reason, string? description, Guid tourId, CancellationToken ct = default)
    {
        var result = await _reviews.ReportAsync(id, reason, description, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Thanks. Our moderation team will review this report.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index), new { tourId });
    }

    private void SetSidebar()
    {
        ViewData["ProviderNav"] = "Reviews";
        ViewBag.Sidebar = new ProviderSidebarVm { DisplayName = User.Identity?.Name ?? "Provider" };
    }
}
