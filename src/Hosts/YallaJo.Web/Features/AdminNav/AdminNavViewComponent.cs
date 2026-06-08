using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Features.AdminNav;

/// <summary>
/// Renders the admin sidebar driven by the DB-backed GET /security/me permission
/// snapshot (plan §9 line 13: "/security/me is the nav driver"). Self-contained and
/// degrades gracefully: on any API failure the sidebar falls back to the request's
/// JWT-claim permissions (ERR3), so the admin shell always renders.
/// </summary>
public sealed class AdminNavViewComponent : ViewComponent
{
    private readonly AdminNavFacade _facade;

    public AdminNavViewComponent(AdminNavFacade facade) => _facade = facade;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var vm = await _facade.GetNavAsync(HttpContext.RequestAborted);
        return View(vm);
    }
}
