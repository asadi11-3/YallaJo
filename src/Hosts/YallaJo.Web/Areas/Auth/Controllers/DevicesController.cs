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
public sealed class DevicesController : BaseController
{
    private readonly DevicesFacade _facade;
    private readonly SessionsFacade _sessionsFacade;
    private readonly IStringLocalizer<YallaJo.Web.Resources.SharedResource> _localizer;

    public DevicesController(
        DevicesFacade facade,
        SessionsFacade sessionsFacade,
        IStringLocalizer<YallaJo.Web.Resources.SharedResource> localizer)
    {
        _facade = facade;
        _sessionsFacade = sessionsFacade;
        _localizer = localizer;
    }

    [HttpGet]
    public IActionResult Index() =>
        RedirectToAction("Index", "Sessions", new { area = "Auth" });

    [HttpPost("auth/devices/trust/{deviceId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Trust(Guid deviceId, CancellationToken ct)
    {
        var result = await _facade.TrustAsync(deviceId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error ?? _localizer["Auth.Sessions.ActionFailed"].Value });
            return await SessionsTablePartialAsync(ct);
        }

        if (result.IsSuccess)
            SetSuccess(_localizer["Auth.Devices.Trusted"]); // CON1: localized flash
        else
            SetError(result.Error);

        return RedirectToAction("Index", "Sessions", new { area = "Auth" });
    }

    /// <summary>Removes the trusted mark — symmetric to Trust (DELETE /api/v1/auth/devices/{id}/trust).</summary>
    [HttpPost("auth/devices/untrust/{deviceId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Untrust(Guid deviceId, CancellationToken ct)
    {
        var result = await _facade.UntrustAsync(deviceId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error ?? _localizer["Auth.Sessions.ActionFailed"].Value });
            return await SessionsTablePartialAsync(ct);
        }

        if (result.IsSuccess)
            SetSuccess(_localizer["Auth.Devices.Untrusted"]); // CON1: localized flash
        else
            SetError(result.Error);

        return RedirectToAction("Index", "Sessions", new { area = "Auth" });
    }

    /// <summary>Fresh sessions table partial for AJAX swaps (PE1 — PRG remains the no-JS path).</summary>
    private async Task<IActionResult> SessionsTablePartialAsync(CancellationToken ct)
    {
        var sessions = await _sessionsFacade.GetSessionsAsync(ct);
        return PartialView("~/Areas/Auth/Views/Sessions/_SessionsTable.cshtml", sessions.Data ?? new SessionsVm());
    }
}
