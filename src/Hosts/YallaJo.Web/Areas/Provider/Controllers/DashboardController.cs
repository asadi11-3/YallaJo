using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

/// <summary>
/// Approved-provider dashboard (<c>/provider/dashboard</c>).
/// <para>
/// Access is checked in-action (not via a hard <c>[RequirePermission]</c>) so that
/// pending applicants — who lack <c>ProviderDashboard.Read</c> — are gently redirected
/// to <c>/provider/status</c> instead of hitting a 403/sign-in page.
/// </para>
/// </summary>
[Area("Provider")]
[Authorize]
public sealed class DashboardController : BaseController
{
    private readonly ProviderDashboardFacade _facade;
    private readonly ICurrentUser _currentUser;

    public DashboardController(ProviderDashboardFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // GET /provider/dashboard
    [HttpGet("provider/dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Pending applicants (User role) don't carry ProviderDashboard.Read — route them
        // to the lifecycle status page rather than triggering a hard 403.
        if (!_currentUser.HasPermission(WebPermission.ProviderDashboard.Read))
            return RedirectToStatus();

        var result = await _facade.GetDashboardAsync(ct);

        return result.Outcome switch
        {
            ProviderDashboardOutcome.Ok => View(result.Dashboard),
            ProviderDashboardOutcome.ForceSignOut => RedirectToLogin(),
            ProviderDashboardOutcome.NoApplication => RedirectToApply(),
            ProviderDashboardOutcome.NotAProvider => RedirectToStatus(),
            ProviderDashboardOutcome.NotApproved => RedirectToStatus(),
            _ => Error(result.Error),
        };
    }

    private IActionResult Error(string? message)
    {
        SetError(message ?? "Could not load your provider dashboard.");
        return RedirectToStatus();
    }

    private IActionResult RedirectToStatus() =>
        RedirectToAction("Status", "Provider", new { area = "Provider" });

    private IActionResult RedirectToApply() =>
        RedirectToAction("Apply", "Provider", new { area = "Provider" });
}
