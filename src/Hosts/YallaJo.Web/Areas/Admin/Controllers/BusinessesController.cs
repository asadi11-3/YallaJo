using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Businesses;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Business.Read)]
public sealed class BusinessesController : BaseController
{
    private readonly BusinessesFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BusinessesController(BusinessesFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid? placeId = null, string? status = null, int page = 1, CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        ViewData["AdminNav"] = "Businesses";
        var result = await _facade.GetIndexAsync(placeId, status, page, ct);
        if (GuardSignOut(result) is { } so)
        {
            return so;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new BusinessesVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/businesses/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Business.Approve)]
    public async Task<IActionResult> Approve(Guid id, Guid? placeId, CancellationToken ct = default)
    {
        var result = await _facade.ApproveAsync(id, ct);
        if (GuardSignOut(result) is { } so)
        {
            return so;
        }

        SetFlash(result, _localizer["Admin.Businesses.Flash.Approved"].Value, _localizer["Admin.Businesses.Flash.ApproveFailed"].Value);
        return Back(placeId);
    }

    [HttpPost("admin/businesses/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Business.Reject)]
    public async Task<IActionResult> Reject(Guid id, Guid? placeId, string? reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.RejectReasonRequired"].Value);
            return Back(placeId);
        }

        var result = await _facade.RejectAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } so)
        {
            return so;
        }

        SetFlash(result, _localizer["Admin.Businesses.Flash.Rejected"].Value, _localizer["Admin.Businesses.Flash.RejectFailed"].Value);
        return Back(placeId);
    }

    [HttpPost("admin/businesses/{id:guid}/request-more-docs")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Business.RequestDocs)]
    public async Task<IActionResult> RequestMoreDocs(Guid id, Guid? placeId, string? reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Businesses.Flash.NoteRequired"].Value);
            return Back(placeId);
        }

        var result = await _facade.RequestMoreDocsAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } so)
        {
            return so;
        }

        SetFlash(result, _localizer["Admin.Businesses.Flash.DocsRequested"].Value, _localizer["Admin.Businesses.Flash.DocsRequestFailed"].Value);
        return Back(placeId);
    }

    [HttpPost("admin/businesses/{id:guid}/suspend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Business.Suspend)]
    public async Task<IActionResult> Suspend(Guid id, Guid? placeId, string? reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.SuspendReasonRequired"].Value);
            return Back(placeId);
        }

        var result = await _facade.SuspendAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } so)
        {
            return so;
        }

        SetFlash(result, _localizer["Admin.Businesses.Flash.Suspended"].Value, _localizer["Admin.Businesses.Flash.SuspendFailed"].Value);
        return Back(placeId);
    }

    [HttpPost("admin/businesses/{id:guid}/reinstate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Business.Reinstate)]
    public async Task<IActionResult> Reinstate(Guid id, Guid? placeId, CancellationToken ct = default)
    {
        var result = await _facade.ReinstateAsync(id, ct);
        if (GuardSignOut(result) is { } so)
        {
            return so;
        }

        SetFlash(result, _localizer["Admin.Businesses.Flash.Reinstated"].Value, _localizer["Admin.Businesses.Flash.ReinstateFailed"].Value);
        return Back(placeId);
    }

    [HttpPost("admin/businesses/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Business.Delete)]
    public async Task<IActionResult> Delete(Guid id, Guid? placeId, CancellationToken ct = default)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } so)
        {
            return so;
        }

        SetFlash(result, _localizer["Admin.Businesses.Flash.Deleted"].Value, _localizer["Admin.Businesses.Flash.DeleteFailed"].Value);
        return Back(placeId);
    }

    private IActionResult Back(Guid? placeId) => RedirectToAction(nameof(Index), new { placeId });
}
