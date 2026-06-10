using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.TourGuides;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class TourGuidesController : ProviderTourResourceController
{
    private readonly ProviderTourGuidesFacade _facade;
    private readonly ICurrentUser _currentUser;

    public TourGuidesController(ProviderTourGuidesFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // ── GET /provider/tours/{id}/guides ───────────────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/guides")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.ReadOwn))
            return RedirectToStatus();

        var result = await _facade.GetIndexAsync(id, ct);
        return result.Outcome switch
        {
            TourGuideOutcome.Ok => View(result.Data),
            TourGuideOutcome.ForceSignOut => RedirectToLogin(),
            TourGuideOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── POST /provider/tours/{id}/guides/assign ───────────────────────────────────
    [HttpPost("provider/tours/{id:guid}/guides/assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid id, AssignTourGuideFormVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourGuide.Update))
            return RedirectToStatus();

        if (!ModelState.IsValid)
            return await ReloadIndex(id, vm, ct);

        var result = await _facade.AssignAsync(id, vm, ct);
        switch (result.Outcome)
        {
            case TourGuideOutcome.Ok:
                SetSuccess("Guide assigned.");
                return RedirectToAction(nameof(Index), new { id });
            case TourGuideOutcome.ForceSignOut:
                return RedirectToLogin();
            case TourGuideOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            case TourGuideOutcome.NotFound:
                SetError(result.Error);
                return RedirectToAction(nameof(Index), new { id });
            default:
                ApplyFacadeValidation(result.ValidationErrors, result.Error, keyPrefix: "Assign.");
                return await ReloadIndex(id, vm, ct);
        }
    }

    // ── POST /provider/tours/{id}/guides/{guideUserId}/remove ─────────────────────
    [HttpPost("provider/tours/{id:guid}/guides/{guideUserId:guid}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id, Guid guideUserId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourGuide.Update))
            return RedirectToStatus();

        var result = await _facade.RemoveAsync(id, guideUserId, ct);
        if (result.Outcome == TourGuideOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == TourGuideOutcome.Ok)
            SetSuccess("Guide removed.");
        else
            SetError(result.Error ?? "Could not remove the guide.");

        return RedirectToAction(nameof(Index), new { id });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private async Task<IActionResult> ReloadIndex(Guid id, AssignTourGuideFormVm assign, CancellationToken ct)
    {
        var result = await _facade.GetIndexAsync(id, ct);
        if (result.Outcome == TourGuideOutcome.ForceSignOut) return RedirectToLogin();
        if (result.Outcome != TourGuideOutcome.Ok || result.Data is null)
            return NotFoundRedirect(result.Error);

        var vm = new TourGuidesIndexVm
        {
            TourId          = result.Data.TourId,
            TourName        = result.Data.TourName,
            TourStatusLabel = result.Data.TourStatusLabel,
            Guides          = result.Data.Guides,
            Assign          = assign,
        };
        return View(nameof(Index), vm);
    }
}
