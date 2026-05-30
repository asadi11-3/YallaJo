using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Areas.Auth.Features.LogoutAll;

[Area("Auth")]
[Authorize]
public sealed class LogoutAllController : Controller
{
    private readonly LogoutAllFacade _facade;
    public LogoutAllController(LogoutAllFacade facade) => _facade = facade;

    [HttpGet]
    public IActionResult Index() => View();


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        await _facade.HandleAsync(ct);
        return RedirectToAction("Index", "Login", new { area = "Auth" });
    }
}
