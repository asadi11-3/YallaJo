using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.JoinRequests;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

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

    public JoinRequestsController(JoinRequestsFacade joinRequests)
    {
        _joinRequests = joinRequests;
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
            SetError("Please check the booking reference, slot, and participant count, then try again.");
            return BackToTab();
        }

        var result = await _joinRequests.SubmitAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your request to join the group was sent.");
        else
            SetError(result.Error ?? "Could not submit your join request.");

        return BackToTab();
    }

    private IActionResult BackToTab()
        => RedirectToAction("Index", "Bookings", new { area = "Accounts", tab = "join-requests" });
}
