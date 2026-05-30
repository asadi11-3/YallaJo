using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.ViewModels;
using YallaJo.Web.Infrastructure.Authorization;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.System.Read)]
public sealed class AuditLogsController : Controller
{
    private readonly AuditLogsFacade _facade;
    public AuditLogsController(AuditLogsFacade facade) => _facade = facade;

    /// <summary>
    /// Phase 5A — paginated admin audit timeline. Filters
    /// (<paramref name="userId"/>, <paramref name="actorUserId"/>,
    /// <paramref name="action"/>, <paramref name="from"/>,
    /// <paramref name="to"/>) are all optional; legacy callers passing
    /// only <paramref name="page"/> / <paramref name="userId"/> behave
    /// exactly as before.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        Guid? userId = null,
        Guid? actorUserId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await _facade.GetLogsAsync(
            page,
            pageSize: 50,
            userId: userId,
            actorUserId: actorUserId,
            action: action,
            from: from,
            to: to,
            ct: ct);

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            // Preserve whatever filters the caller supplied so the view
            // can re-render the filter bar correctly even on error.
            return View(new AuditLogListVm
            {
                FilterUserId      = userId,
                FilterActorUserId = actorUserId,
                FilterAction      = action,
                FilterFrom        = from,
                FilterTo          = to,
            });
        }

        return View(result.Data);
    }
}
