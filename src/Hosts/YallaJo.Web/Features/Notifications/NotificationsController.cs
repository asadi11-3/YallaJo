using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Features.Notifications;

/// <summary>
/// Backs the shared notification-bell mark-read actions. Lives in the default (no) area.
/// </summary>
[Authorize]
public sealed class NotificationsController : BaseController
{
    private readonly NotificationsFacade _facade;

    public NotificationsController(NotificationsFacade facade) => _facade = facade;

    [HttpPost("notifications/read")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Notification.Update)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await _facade.MarkReadAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            SetError(result.Error);
        }

        return RedirectBack();
    }

    [HttpPost("notifications/read-all")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Notification.Update)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var result = await _facade.MarkAllReadAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "All notifications marked as read.", "Could not mark notifications as read.");
        return RedirectBack();
    }

    private IActionResult RedirectBack()
    {
        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrWhiteSpace(referer) && Url.IsLocalUrl(referer))
        {
            return Redirect(referer);
        }

        return RedirectToAction("Index", "Profile", new { area = "Accounts" });
    }
}
