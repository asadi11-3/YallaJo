using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Reviews;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
[RequirePermission(WebPermission.Review.Read)]
public sealed class ReviewsController : BaseController
{
    private readonly ReviewsFacade _reviews;
    private readonly ProfileFacade _profile;

    public ReviewsController(ReviewsFacade reviews, ProfileFacade profile)
    {
        _reviews = reviews;
        _profile = profile;
    }

    [HttpGet("accounts/reviews")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AccountNav"] = "Reviews";
        await PopulateSidebarAsync(ct);

        var result = await _reviews.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ReviewsVm());
        }

        return View(result.Data);
    }

    [HttpPost("accounts/reviews/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Review.Update)]
    public async Task<IActionResult> Edit(EditReviewFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please correct the highlighted fields and try again.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _reviews.EditAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your review has been updated.");
        else
            SetError(result.Error ?? "Could not save your changes.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("accounts/reviews/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Review.Delete)]
    public async Task<IActionResult> Delete(Guid id, string? rowVersion, CancellationToken ct)
    {
        var result = await _reviews.DeleteAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your review has been deleted.");
        else
            SetError(result.Error ?? "Could not delete the review.");

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSidebarAsync(CancellationToken ct)
    {
        var profile = await _profile.GetAsync(ct);
        if (profile is { IsSuccess: true, Data: { } p })
        {
            ViewBag.Sidebar = new AccountSidebarVm
            {
                AvatarUrl = p.AvatarUrl,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName)
                    ? $"{p.FirstName} {p.LastName}".Trim()
                    : p.DisplayName,
                Email = p.Email,
            };
        }
        else
        {
            ViewBag.Sidebar = new AccountSidebarVm();
        }
    }
}
