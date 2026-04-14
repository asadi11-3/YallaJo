using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Claims;

/// <summary>
/// Claims are managed from the Users and Roles detail pages.
/// This controller redirects to the Users list so that the route /admin/claims
/// does not result in a 404.
/// </summary>
[Area("Admin")]
[Authorize]
public sealed class ClaimsController : Controller
{
    [HttpGet]
    public IActionResult Index() =>
        RedirectToAction("Index", "Users", new { area = "Admin" });
}
