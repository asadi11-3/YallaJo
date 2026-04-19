using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Areas.Auth.Features.Devices;

/// <summary>
/// Exposes the TrustDevice action (PATCH /api/v1/auth/devices/{deviceId}/trust).
/// Called via form POST from the Sessions page.
/// No dedicated GET page — device info surfaces through Sessions.
/// </summary>
[Area("Auth")]
[Authorize]
public sealed class DevicesController : Controller
{
    private readonly DevicesFacade _facade;
    public DevicesController(DevicesFacade facade) => _facade = facade;

    // GET /auth/devices — landing stub (redirects to sessions)
    [HttpGet]
    public IActionResult Index() =>
        RedirectToAction("Index", "Sessions", new { area = "Auth" });

    // POST /auth/devices/trust/{deviceId}
    [HttpPost("auth/devices/trust/{deviceId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Trust(Guid deviceId, CancellationToken ct)
    {
        var result = await _facade.TrustAsync(deviceId, ct);

        if (result.RequireSignOut)
            return RedirectToAction("Index", "Login", new { area = "Auth" });

        TempData["DeviceMessage"] = result.IsSuccess
            ? "Device marked as trusted."
            : result.Error;

        return RedirectToAction("Index", "Sessions", new { area = "Auth" });
    }
}
