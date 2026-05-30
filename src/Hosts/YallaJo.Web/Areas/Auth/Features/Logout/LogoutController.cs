using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Areas.Auth.Features.Logout;

[Area("Auth")]
[Authorize]
public sealed class LogoutController : Controller
{
    private readonly LogoutFacade _facade;
    public LogoutController(LogoutFacade facade) => _facade = facade;

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        await _facade.HandleAsync(ct);
        return RedirectToAction("Index", "Login", new { area = "Auth" });
    }
}
