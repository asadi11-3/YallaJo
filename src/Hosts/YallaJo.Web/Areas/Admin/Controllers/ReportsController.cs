using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Reports;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminModerationQueue.Read)]
public sealed class ReportsController : BaseController
{
    private readonly ReportsFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ReportsController(ReportsFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ReportsFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Reports";
        // UI-UX-D1/R4: cap the page size at 50 at the controller boundary.
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var result = await _facade.GetIndexAsync(request.AfterCursor, pageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ReportsVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/reports/{id:guid}/resolve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminModerationQueue.Resolve)]
    public async Task<IActionResult> Resolve(Guid id, string? action, string? notes, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            SetError(_localizer["Admin.Reports.Flash.ActionRequired"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.ResolveAsync(id, action.Trim(), notes, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Reports.Flash.Resolved"].Value, _localizer["Admin.Reports.Flash.ResolveFailed"].Value);
        return RedirectToAction(nameof(Index));
    }
}
