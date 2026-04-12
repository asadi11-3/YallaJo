using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Areas.Auth.Features.Sessions;

[Area("Auth")]
[Authorize]
public sealed class SessionsController : Controller
{
    private readonly SessionsFacade _facade;
    public SessionsController(SessionsFacade facade) => _facade = facade;

    // GET /auth/sessions
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _facade.GetSessionsAsync(ct);

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new ViewModels.SessionsVm());
        }

        return View(result.Data);
    }

    // POST /auth/sessions/revoke/{sessionId}
    [HttpPost("auth/sessions/revoke/{sessionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(Guid sessionId, CancellationToken ct)
    {
        var result = await _facade.RevokeAsync(sessionId, ct);

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        return RedirectToAction(nameof(Index));
    }
}
