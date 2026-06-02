using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.AuditLogs;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.System.Read)]
public sealed class AuditLogsController : BaseController
{
    private readonly AuditLogsFacade _facade;
    public AuditLogsController(AuditLogsFacade facade) => _facade = facade;

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

        if (GuardSignOut(result) is { } signOut) return signOut;

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
