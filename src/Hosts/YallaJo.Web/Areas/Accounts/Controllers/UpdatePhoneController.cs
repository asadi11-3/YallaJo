using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// Phase 2 (Accounts plan): the standalone update-phone page was retired and merged into the
/// Settings hub's Security tab. The GET 301s there; the phone write now lives solely at
/// POST /accounts/settings/phone (SettingsController.UpdatePhone) — the duplicate POST that
/// previously lived here had no remaining consumers after the page retirement.
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class UpdatePhoneController : BaseController
{
    [HttpGet]
    public IActionResult Index()
        => RedirectPermanent(Url.Action("Index", "Settings", new { area = "Accounts", tab = "security" })!);
}
