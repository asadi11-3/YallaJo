using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Areas.Auth.Features.Devices;

[Area("Auth")]
[Authorize]
public sealed class DevicesController : Controller
{
    private readonly DevicesFacade _facade;
    public DevicesController(DevicesFacade facade) => _facade = facade;

    [HttpGet]
    public IActionResult Index() =>
        RedirectToAction("Index", "Sessions", new { area = "Auth" });

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
