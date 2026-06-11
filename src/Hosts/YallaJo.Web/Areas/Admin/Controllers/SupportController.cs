using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Support;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.SupportTicket.Read)]
public sealed class SupportController : BaseController
{
    private readonly SupportFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SupportController(SupportFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] SupportFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Support";
        // UI-UX-D1/R4: cap the page size at 50 at the controller boundary.
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var result = await _facade.GetIndexAsync(request.Status, request.Category, request.Cursor, pageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new SupportListVm());
        }

        return View(result.Data);
    }

    [HttpGet("admin/support/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Support";
        var result = await _facade.GetDetailsAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("admin/support/{id:guid}/messages")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.SupportTicket.Read)]
    public async Task<IActionResult> Reply(Guid id, string? body, bool isInternal, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            SetError(_localizer["Admin.Support.Flash.ReplyRequired"].Value);
            return Back(id);
        }

        var result = await _facade.PostMessageAsync(id, body.Trim(), isInternal, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Support.Flash.ReplyPosted"].Value, _localizer["Admin.Support.Flash.ReplyFailed"].Value);
        return Back(id);
    }

    [HttpPost("admin/support/{id:guid}/close")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.SupportTicket.Close)]
    public async Task<IActionResult> Close(Guid id, string? rowVersion, CancellationToken ct)
    {
        var result = await _facade.CloseAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Support.Flash.TicketClosed"].Value, _localizer["Admin.Support.Flash.TicketCloseFailed"].Value);
        return Back(id);
    }

    [HttpPost("admin/support/{id:guid}/assign")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminSupportQueue.Assign)]
    public async Task<IActionResult> Assign(Guid id, Guid adminUserId, CancellationToken ct)
    {
        if (adminUserId == Guid.Empty)
        {
            SetError(_localizer["Admin.Support.Flash.AdminIdRequired"].Value);
            return Back(id);
        }

        var result = await _facade.AssignAsync(id, adminUserId, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Support.Flash.TicketAssigned"].Value, _localizer["Admin.Support.Flash.TicketAssignFailed"].Value);
        return Back(id);
    }

    [HttpPost("admin/support/{id:guid}/resolve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminSupportQueue.Resolve)]
    public async Task<IActionResult> Resolve(Guid id, string? notes, string? rowVersion, CancellationToken ct)
    {
        var result = await _facade.ResolveAsync(id, notes, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.Support.Flash.TicketResolved"].Value, _localizer["Admin.Support.Flash.TicketResolveFailed"].Value);
        return Back(id);
    }

    private IActionResult Back(Guid id) => RedirectToAction(nameof(Details), new { id });
}
