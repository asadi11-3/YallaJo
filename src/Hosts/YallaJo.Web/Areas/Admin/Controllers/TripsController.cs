using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Trips;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin Tour-approvals screen: look up a tour by id to load its privileged detail (which carries the
/// RowVersion concurrency token) and moderate it (approve/reject/suspend/reinstate). A supplementary
/// approved-tours browse table is display-only.
/// </summary>
[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Tour.Approve)]
public sealed class TripsController : BaseController
{
    private const int DefaultPageSize = 20;
    private readonly TripsFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public TripsController(TripsFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? id = null, int page = 1, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        ViewData["AdminNav"] = "Tours";

        var result = await _facade.GetIndexAsync(id, page, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new TripsVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/trips/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Approve)]
    public async Task<IActionResult> Approve(Guid id, string rowVersion, CancellationToken ct)
    {
        var result = await _facade.ApproveAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, _localizer["Admin.Shared.Flash.TourApproved"].Value, _localizer["Admin.Shared.Flash.TourApproveFailed"].Value);
        return Back(id);
    }

    [HttpPost("admin/trips/{id:guid}/reinstate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Reinstate)]
    public async Task<IActionResult> Reinstate(Guid id, string rowVersion, CancellationToken ct)
    {
        var result = await _facade.ReinstateAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, _localizer["Admin.Shared.Flash.TourReinstated"].Value, _localizer["Admin.Shared.Flash.TourReinstateFailed"].Value);
        return Back(id);
    }

    [HttpPost("admin/trips/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Reject)]
    public async Task<IActionResult> Reject(Guid id, string rowVersion, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.RejectReasonRequired"].Value);
            return Back(id);
        }

        var result = await _facade.RejectAsync(id, rowVersion, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, _localizer["Admin.Shared.Flash.TourRejected"].Value, _localizer["Admin.Shared.Flash.TourRejectFailed"].Value);
        return Back(id);
    }

    [HttpPost("admin/trips/{id:guid}/suspend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Suspend)]
    public async Task<IActionResult> Suspend(Guid id, string rowVersion, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.SuspendReasonRequired"].Value);
            return Back(id);
        }

        var result = await _facade.SuspendAsync(id, rowVersion, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, _localizer["Admin.Shared.Flash.TourSuspended"].Value, _localizer["Admin.Shared.Flash.TourSuspendFailed"].Value);
        return Back(id);
    }

    private IActionResult Back(Guid id) => RedirectToAction(nameof(Index), new { id });
}
