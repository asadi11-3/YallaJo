using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs;

/// <summary>Admin audit log viewer — paginated, optional user filter.</summary>
[Area("Admin")]
[Authorize]
public sealed class AuditLogsController : Controller
{
    private readonly AuditLogsFacade _facade;
    public AuditLogsController(AuditLogsFacade facade) => _facade = facade;

    // GET /admin/auditlogs?page=1&userId={guid}
    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1, Guid? userId = null, CancellationToken ct = default)
    {
        var result = await _facade.GetLogsAsync(page, pageSize: 50, userId, ct);

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new AuditLogListVm());
        }

        return View(result.Data);
    }
}
