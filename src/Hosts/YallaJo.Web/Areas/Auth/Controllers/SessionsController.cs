using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Areas.Auth.Facades;
using YallaJo.Web.Areas.Auth.Models.Sessions;

namespace YallaJo.Web.Areas.Auth.Controllers;
[Area("Auth")]
[Authorize]
public sealed class SessionsController : BaseController
{
    private readonly SessionsFacade _facade;
    public SessionsController(SessionsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _facade.GetSessionsAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return View(new SessionsVm());
        }

        return View(result.Data);
    }

    [HttpPost("auth/sessions/revoke/{sessionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(Guid sessionId, CancellationToken ct)
    {
        var result = await _facade.RevokeAsync(sessionId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Session revoked.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }
}
