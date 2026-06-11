using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.JoinRequests;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// Phase 3 (Accounts master plan): join requests now live as a tab on My Trips
/// (/accounts/bookings?tab=join-requests). Index permanently redirects there; Create
/// keeps its route, anti-forgery and permission gate, and PRGs back to the tab.
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class JoinRequestsController : BaseController
{
    private readonly JoinRequestsFacade _joinRequests;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public JoinRequestsController(JoinRequestsFacade joinRequests, IStringLocalizer<SharedResource> localizer)
    {
        _joinRequests = joinRequests;
        _localizer = localizer;
    }

    [HttpGet("accounts/join-requests")]
    public IActionResult Index()
        => RedirectPermanent(Url.Action("Index", "Bookings", new { area = "Accounts", tab = "join-requests" })!);

    [HttpPost("accounts/join-requests")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.JoinRequest.Create)]
    public async Task<IActionResult> Create(JoinRequestFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError(_localizer["Accounts.Msg.JoinRequestFormError"]);
            return BackToTab();
        }

        var result = await _joinRequests.SubmitAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.JoinRequestSent"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.JoinRequestFailed"].Value);

        return BackToTab();
    }

    private IActionResult BackToTab()
        => RedirectToAction("Index", "Bookings", new { area = "Accounts", tab = "join-requests" });
}
