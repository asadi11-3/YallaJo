using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Shared;

namespace YallaJo.Web.Areas.Provider.ViewComponents;

/// <summary>
/// Renders the provider dashboard chrome (avatar card + pill nav) once per page.
/// Replaces the per-controller SetSidebar() helpers and per-view
/// "ViewBag.Sidebar as ProviderSidebarVm ?? new ..." boilerplate.
/// Active-nav highlighting still flows through ViewData["ProviderNav"], which
/// each page view sets alongside its title.
/// </summary>
public sealed class ProviderSidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var vm = new ProviderSidebarVm
        {
            // Empty falls back to the localized Provider.Nav.DefaultName inside the view.
            DisplayName = HttpContext.User.Identity?.Name ?? string.Empty,
        };

        return View("~/Areas/Provider/Shared/_ProviderSidebar.cshtml", vm);
    }
}
