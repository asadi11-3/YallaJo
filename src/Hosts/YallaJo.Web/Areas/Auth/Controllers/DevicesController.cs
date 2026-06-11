using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Areas.Auth.Facades;

namespace YallaJo.Web.Areas.Auth.Controllers;
[Area("Auth")]
[Authorize]
public sealed class DevicesController : BaseController
{
    private readonly DevicesFacade _facade;
    private readonly IStringLocalizer<YallaJo.Web.Resources.SharedResource> _localizer;

    public DevicesController(
        DevicesFacade facade,
        IStringLocalizer<YallaJo.Web.Resources.SharedResource> localizer)
    {
        _facade = facade;
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

        if (result.IsSuccess)
            SetSuccess(_localizer["Auth.Devices.Trusted"]); // CON1: localized flash
        else
            SetError(result.Error);

        return RedirectToAction("Index", "Sessions", new { area = "Auth" });
    }
}
