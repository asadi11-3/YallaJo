using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Notifications;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Features.Notifications;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class NotificationsController : BaseController
{
    private readonly NotificationsFacade _notifications;
    private readonly ProfileFacade _profile;

    public NotificationsController(NotificationsFacade notifications, ProfileFacade profile)
    {
        _notifications = notifications;
        _profile = profile;
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
        await PopulateSidebarAsync(ct);

        var filter = new NotificationInboxFilterVm
        {
            Status = NormalizeStatus(status),
            Type = string.IsNullOrWhiteSpace(type) ? null : type,
            FromDate = fromDate,
            ToDate = toDate,
        };

        var vm = await _notifications.GetInboxAsync(filter, cursor, ct);
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

        if (result.IsSuccess)
            SetSuccess("Notification deleted.");
        else
            SetError(result.Error ?? "Could not delete the notification.");

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
