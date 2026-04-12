using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Areas.Auth.Features.LogoutAll;

/// <summary>POST /auth/logoutall — revokes all sessions, clears cookie, redirects to login.</summary>
[Area("Auth")]
[Authorize]
public sealed class LogoutAllController : Controller
{
    private readonly LogoutAllFacade _facade;
    public LogoutAllController(LogoutAllFacade facade) => _facade = facade;

    // GET /auth/logoutall
    [HttpGet]
    public IActionResult Index() => View();

    // POST /auth/logoutall
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        await _facade.HandleAsync(ct);
        return RedirectToAction("Index", "Login", new { area = "Auth" });
    }
}
