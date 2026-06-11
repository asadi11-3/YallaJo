using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// FE-1C — authenticated user-facing Support Tickets pages.
/// Creation is intentionally NOT here: users open new tickets via the existing
/// /contact form (the list page links to it). This controller covers list / detail /
/// reply / close for the caller's OWN tickets; the backend auto-scopes ownership.
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class SupportController : BaseController
{
    private readonly SupportFacade _support;
    private readonly ProfileFacade _profile;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SupportController(SupportFacade support, ProfileFacade profile, IStringLocalizer<SharedResource> localizer)
    {
        _support = support;
        _profile = profile;
        _localizer = localizer;
    }

    [HttpGet("accounts/support")]
    [RequirePermission(WebPermission.SupportTicket.Read)]
    public async Task<IActionResult> Index(Guid? cursor, CancellationToken ct)
    {
        ViewData["AccountNav"] = "Support";
        await PopulateSidebarAsync(ct);

        var vm = await _support.GetListAsync(cursor, ct);
        return View(vm);
    }

    [HttpGet("accounts/support/{id:guid}")]
    [RequirePermission(WebPermission.SupportTicket.Read)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        ViewData["AccountNav"] = "Support";
        await PopulateSidebarAsync(ct);

        var result = await _support.GetDetailAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("accounts/support/{id:guid}/messages")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.SupportTicket.Read)]
    public async Task<IActionResult> Reply(Guid id, string? body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            // Phase 4d: AJAX callers get a JSON error; no-JS callers get the PRG flash.
            if (WantsAjax()) return BadRequest(new { error = _localizer["Accounts.Msg.ReplyEmpty"].Value });
            SetError(_localizer["Accounts.Msg.ReplyEmpty"]);
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _support.PostMessageAsync(id, body.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        // Phase 4d: AJAX reply returns the refreshed thread partial; PRG remains the no-JS path (PE1).
        if (WantsAjax())
        {
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error ?? _localizer["Accounts.Msg.ReplyFailed"].Value });

            var refreshed = await _support.GetDetailAsync(id, ct);
            return refreshed is { IsSuccess: true, Data: { } detail }
                ? PartialView("_SupportThread", detail)
                : BadRequest(new { error = refreshed.Error ?? _localizer["Accounts.Msg.ReplyFailed"].Value });
        }

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.ReplySent"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.ReplyFailed"].Value);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("accounts/support/{id:guid}/close")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.SupportTicket.Close)]
    public async Task<IActionResult> Close(Guid id, string? rowVersion, CancellationToken ct)
    {
        var result = await _support.CloseAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.TicketClosed"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.TicketCloseFailed"].Value);

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateSidebarAsync(CancellationToken ct)
    {
        var profile = await _profile.GetAsync(ct);
        if (profile is { IsSuccess: true, Data: { } p })
        {
            ViewBag.Sidebar = new AccountSidebarVm
            {
                AvatarUrl = p.AvatarUrl,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName)
                    ? $"{p.FirstName} {p.LastName}".Trim()
                    : p.DisplayName,
                Email = p.Email,
            };
        }
        else
        {
            ViewBag.Sidebar = new AccountSidebarVm();
        }
    }
}
