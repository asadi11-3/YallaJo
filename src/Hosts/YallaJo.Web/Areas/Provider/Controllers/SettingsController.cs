using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class SettingsController : BaseController
{
    private readonly ProviderSettingsFacade _facade;
    private readonly ICurrentUser _currentUser;

    public SettingsController(ProviderSettingsFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // GET /provider/settings
    [HttpGet("provider/settings")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Approved-provider marker; pending applicants (User role) don't carry it and
        // are routed to the lifecycle status page instead of a hard 403.
        if (!_currentUser.HasPermission(WebPermission.ProviderDashboard.Read))
            return RedirectToStatus();

        var result = await _facade.GetSettingsAsync(ct);

        return result.Outcome switch
        {
            ProviderSettingsOutcome.Ok => View(result.Settings),
            ProviderSettingsOutcome.ForceSignOut => RedirectToLogin(),
            ProviderSettingsOutcome.NoApplication => RedirectToApply(),
            _ => Error(result.Error),
        };
    }

    private IActionResult Error(string? message)
    {
        SetError(message ?? "Could not load your provider settings.");
        return RedirectToStatus();
    }

    private IActionResult RedirectToStatus() =>
        RedirectToAction("Status", "Provider", new { area = "Provider" });

    private IActionResult RedirectToApply() =>
        RedirectToAction("Apply", "Provider", new { area = "Provider" });
}
