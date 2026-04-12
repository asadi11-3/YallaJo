using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Areas.Auth.Features.Logout;

/// <summary>
/// POST /auth/logout — revokes backend session, clears cookie, redirects to login.
/// GET  /auth/logout — shows a minimal confirmation page so the logout button
///                     can be a proper anti-forgery-protected form.
/// </summary>
[Area("Auth")]
[Authorize]
public sealed class LogoutController : Controller
{
    private readonly LogoutFacade _facade;
    public LogoutController(LogoutFacade facade) => _facade = facade;

    // GET /auth/logout
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Index() => View();

    // POST /auth/logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        await _facade.HandleAsync(ct);
        return RedirectToAction("Index", "Login", new { area = "Auth" });
    }
}
