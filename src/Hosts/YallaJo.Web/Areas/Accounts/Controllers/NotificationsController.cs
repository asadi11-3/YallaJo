using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Notifications;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Features.Notifications;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class NotificationsController : BaseController
{
    private readonly NotificationsFacade _notifications;
    private readonly ProfileFacade _profile;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public NotificationsController(NotificationsFacade notifications, ProfileFacade profile, IStringLocalizer<SharedResource> localizer)
    {
        _notifications = notifications;
        _profile = profile;
        _localizer = localizer;
    }

    [HttpGet("accounts/notifications")]
    [RequirePermission(WebPermission.Notification.Read)]
    public async Task<IActionResult> Index(
        string? status,
        string? type,
        string? fromDate,
        string? toDate,
        Guid? cursor,
        CancellationToken ct)
    {
        ViewData["AccountNav"] = "Notifications";

        var filter = new NotificationInboxFilterVm
        {
            Status = NormalizeStatus(status),
            Type = string.IsNullOrWhiteSpace(type) ? null : type,
            FromDate = fromDate,
            ToDate = toDate,
        };

        var vm = await _notifications.GetInboxAsync(filter, cursor, ct);

        // Phase 4: AJAX "Load more" (accounts-notifications.js) receives only the list fragment (UI-UX-S1/PE1).
        if (WantsAjax())
            return PartialView("_NotificationsList", vm);

        await PopulateSidebarAsync(ct);
        if (vm.LoadError is not null)
        {
            SetError(vm.LoadError);
        }

        return View(vm);
    }

    [HttpPost("accounts/notifications/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Notification.Delete)]
    public async Task<IActionResult> Delete(
        Guid id,
        string? status,
        string? type,
        string? fromDate,
        string? toDate,
        CancellationToken ct)
    {
        var result = await _notifications.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error ?? _localizer["Accounts.Msg.NotificationDeleteFailed"].Value });

            var filter = new NotificationInboxFilterVm
            {
                Status = NormalizeStatus(status),
                Type = string.IsNullOrWhiteSpace(type) ? null : type,
                FromDate = fromDate,
                ToDate = toDate,
            };
            var vm = await _notifications.GetInboxAsync(filter, null, ct);
            return PartialView("_NotificationsList", vm);
        }

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.NotificationDeleted"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.NotificationDeleteFailed"].Value);

        // Preserve the active filters when returning to the inbox.
        return RedirectToAction(nameof(Index), new { status, type, fromDate, toDate });
    }

    private static string NormalizeStatus(string? status) =>
        NotificationInboxFilterVm.StatusOptions
            .FirstOrDefault(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase)) ?? "all";

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
