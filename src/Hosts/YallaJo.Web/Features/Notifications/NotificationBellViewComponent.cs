using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Features.Notifications;

/// <summary>
/// Renders the shared notification-bell dropdown in the header. Self-contained:
/// fetches unread count + recent notifications server-side. Degrades gracefully
/// (empty bell) on any API failure, so it can be safely embedded in every layout.
/// </summary>
public sealed class NotificationBellViewComponent : ViewComponent
{
    private readonly NotificationsFacade _facade;

    public NotificationBellViewComponent(NotificationsFacade facade) => _facade = facade;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var vm = await _facade.GetBellAsync(HttpContext.RequestAborted);
        return View(vm);
    }
}
