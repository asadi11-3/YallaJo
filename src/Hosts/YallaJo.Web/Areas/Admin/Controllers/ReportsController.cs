using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Reports;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminModerationQueue.Read)]
public sealed class ReportsController : BaseController
{
    private readonly ReportsFacade _facade;

    public ReportsController(ReportsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ReportsFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Reports";
        var result = await _facade.GetIndexAsync(request.AfterCursor, request.PageSize, ct);
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
            SetError("A moderation action is required.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.ResolveAsync(id, action.Trim(), notes, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Report resolved.", "Could not resolve the report.");
        return RedirectToAction(nameof(Index));
    }
}
