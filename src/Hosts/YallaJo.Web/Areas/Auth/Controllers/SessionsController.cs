using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Areas.Auth.Facades;
using YallaJo.Web.Areas.Auth.Models.Sessions;

namespace YallaJo.Web.Areas.Auth.Controllers;
[Area("Auth")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)] // C2: authenticated per-user data
public sealed class SessionsController : BaseController
{
    private readonly SessionsFacade _facade;
    private readonly IStringLocalizer<YallaJo.Web.Resources.SharedResource> _localizer;

    public SessionsController(
        SessionsFacade facade,
        IStringLocalizer<YallaJo.Web.Resources.SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _facade.GetSessionsAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return WantsAjax()
                ? PartialView("_SessionsTable", new SessionsVm())
                : View(new SessionsVm());
        }

        // PE1: AJAX callers (api-client.js) get just the table partial; everyone else the full page.
        return WantsAjax() ? PartialView("_SessionsTable", result.Data) : View(result.Data);
    }

    [HttpPost("auth/sessions/revoke/{sessionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(Guid sessionId, CancellationToken ct)
    {
        var result = await _facade.RevokeAsync(sessionId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error ?? _localizer["Auth.Sessions.ActionFailed"].Value });
            return await SessionsTablePartialAsync(ct);
        }

        if (result.IsSuccess)
            SetSuccess(_localizer["Auth.Sessions.Revoked"]); // CON1: localized flash
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Signs the user out everywhere except the current device. Backed by
    /// POST /api/v1/auth/sessions/revoke-others (non-destructive to the current session,
    /// unlike sign-out-all which also ends the caller's session).
    /// </summary>
    [HttpPost("auth/sessions/revoke-others")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeOthers(CancellationToken ct)
    {
        var result = await _facade.RevokeOthersAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error ?? _localizer["Auth.Sessions.ActionFailed"].Value });
            return await SessionsTablePartialAsync(ct);
        }

        if (result.IsSuccess)
            SetSuccess(_localizer["Auth.Sessions.OthersRevoked", result.Data]); // CON1: localized flash with count
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Fresh sessions table partial for AJAX swaps (PE1 — PRG remains the no-JS path).</summary>
    private async Task<IActionResult> SessionsTablePartialAsync(CancellationToken ct)
    {
        var sessions = await _facade.GetSessionsAsync(ct);
        return PartialView("_SessionsTable", sessions.Data ?? new SessionsVm());
    }
}
