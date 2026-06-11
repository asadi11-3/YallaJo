using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Disputes;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminFinanceDashboard.Read)]
public sealed class DisputesController(DisputesFacade facade, IStringLocalizer<SharedResource> localizer) : BaseController
{
    private readonly DisputesFacade _facade = facade;
    private readonly IStringLocalizer<SharedResource> _localizer = localizer;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] DisputesFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Disputes";
        var result = await _facade.GetIndexAsync(request.Status, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new DisputesVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/disputes/{id:guid}/review")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminFinanceDashboard.Update)]
    public async Task<IActionResult> Review(Guid id, CancellationToken ct)
    {
        var result = await _facade.ReviewAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Disputes.Flash.UnderReview"].Value, _localizer["Admin.Disputes.Flash.UnderReviewFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/disputes/{id:guid}/resolve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminFinanceDashboard.Approve)]
    public async Task<IActionResult> Resolve(Guid id, string? resolution, string? notes, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(resolution))
        {
            SetError(_localizer["Admin.Disputes.Flash.ResolutionRequired"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.ResolveAsync(id, resolution, notes, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Shared.Flash.DisputeResolved"].Value, _localizer["Admin.Shared.Flash.DisputeResolveFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/disputes/{id:guid}/escalate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminFinanceDashboard.Update)]
    public async Task<IActionResult> Escalate(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Disputes.Flash.EscalationReasonRequired"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.EscalateAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Disputes.Flash.Escalated"].Value, _localizer["Admin.Disputes.Flash.EscalateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }
}
