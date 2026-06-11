using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Moderation;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.ContentModerationLog.Read)]
public sealed class ModerationController : BaseController
{
    private readonly ModerationFacade _facade;
    private readonly LookupsFacade _lookups;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ModerationController(ModerationFacade facade, LookupsFacade lookups, IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
        _facade = facade;
        _lookups = lookups;
    }

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

        // S1/PE1: the same action serves the full page and the listing.js fragment
        // (WantsAjax = X-Requested-With: fetch). No [OutputCache] ever (C2).
        if (!result.IsSuccess || result.Data is null)
        {
            var fallback = new ModerationVm();
            if (WantsAjax())
            {
                ViewBag.Error = result.Error;
                return PartialView("_ModerationResults", fallback);
            }

            SetError(result.Error);
            return View(fallback);
        }

        return WantsAjax() ? PartialView("_ModerationResults", result.Data) : View(result.Data);
    }

    [HttpPost("admin/moderation/warn")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminModerationQueue.Warn)]
    public async Task<IActionResult> Warn(WarnUserFormVm form, [FromForm] string? userQuery, CancellationToken ct)
    {
        // PE1: without JS the hidden id stays empty — resolve the typed query server-side (F10).
        form.UserId = await ResolveUserAsync(form.UserId, userQuery, ct) ?? Guid.Empty;
        if (!ModelState.IsValid || form.UserId == Guid.Empty)
        {
            SetError(_localizer["Admin.Moderation.Flash.WarnFieldsRequired"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.WarnAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Moderation.Flash.WarningIssued"].Value, _localizer["Admin.Moderation.Flash.WarnFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/moderation/ban")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminModerationQueue.Ban)]
    public async Task<IActionResult> Ban(BanUserFormVm form, [FromForm] string? userQuery, CancellationToken ct)
    {
        // PE1: without JS the hidden id stays empty — resolve the typed query server-side (F10).
        form.UserId = await ResolveUserAsync(form.UserId, userQuery, ct) ?? Guid.Empty;
        if (!ModelState.IsValid || form.UserId == Guid.Empty)
        {
            SetError(_localizer["Admin.Moderation.Flash.BanFieldsRequired"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.BanAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Moderation.Flash.UserBanned"].Value, _localizer["Admin.Moderation.Flash.BanFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/moderation/unban")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminModerationQueue.Ban)]
    public async Task<IActionResult> Unban([FromForm] Guid userId, [FromForm] string? userQuery, CancellationToken ct)
    {
        // PE1: without JS the hidden id stays empty — resolve the typed query server-side (F10).
        userId = await ResolveUserAsync(userId, userQuery, ct) ?? Guid.Empty;
        if (userId == Guid.Empty)
        {
            SetError(_localizer["Admin.Moderation.Flash.UserNotFound"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.UnbanAsync(userId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Moderation.Flash.BanLifted"].Value, _localizer["Admin.Moderation.Flash.UnbanFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// F10/PE1 fallback resolution: keeps a JS-provided id, otherwise accepts a pasted
    /// GUID, otherwise resolves the typed text via the users suggest lookup.
    /// </summary>
    private async Task<Guid?> ResolveUserAsync(Guid current, string? userQuery, CancellationToken ct)
    {
        if (current != Guid.Empty)
        {
            return current;
        }

        if (string.IsNullOrWhiteSpace(userQuery))
        {
            return null;
        }

        if (Guid.TryParse(userQuery.Trim(), out var parsed))
        {
            return parsed;
        }

        return await _lookups.ResolveUserIdAsync(userQuery, ct);
    }
}
