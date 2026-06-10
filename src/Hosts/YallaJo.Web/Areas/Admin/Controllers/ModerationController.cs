using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Moderation;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.ContentModerationLog.Read)]
public sealed class ModerationController : BaseController
{
    private readonly ModerationFacade _facade;

    public ModerationController(ModerationFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ModerationFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Moderation";
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
            return View(new ModerationVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/moderation/warn")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminModerationQueue.Warn)]
    public async Task<IActionResult> Warn(WarnUserFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please provide a user, entity, and reason for the warning.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.WarnAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Warning issued.", "Could not issue the warning.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/moderation/ban")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminModerationQueue.Ban)]
    public async Task<IActionResult> Ban(BanUserFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please provide a user, entity, and reason for the ban.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.BanAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "User banned.", "Could not issue the ban.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/moderation/unban")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminModerationQueue.Ban)]
    public async Task<IActionResult> Unban([FromForm] Guid userId, CancellationToken ct)
    {
        var result = await _facade.UnbanAsync(userId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Ban lifted.", "Could not lift the ban.");
        return RedirectToAction(nameof(Index));
    }
}
