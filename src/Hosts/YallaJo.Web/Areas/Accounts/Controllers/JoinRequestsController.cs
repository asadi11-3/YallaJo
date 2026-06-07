using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.JoinRequests;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class JoinRequestsController : BaseController
{
    private readonly JoinRequestsFacade _joinRequests;
    private readonly ProfileFacade _profile;

    public JoinRequestsController(JoinRequestsFacade joinRequests, ProfileFacade profile)
    {
        _joinRequests = joinRequests;
        _profile = profile;
    }

    [HttpGet("accounts/join-requests")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        ViewData["AccountNav"] = "JoinRequests";
        await PopulateSidebarAsync(ct);

        var result = await _joinRequests.GetMineAsync(ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new MyJoinRequestsVm());
        }

        return View(result.Data);
    }

    [HttpPost("accounts/join-requests")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.JoinRequest.Create)]
    public async Task<IActionResult> Create(JoinRequestFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please check the booking reference, slot, and participant count, then try again.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _joinRequests.SubmitAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your request to join the group was sent.");
        else
            SetError(result.Error ?? "Could not submit your join request.");

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSidebarAsync(CancellationToken ct)
    {
        var profile = await _profile.GetAsync(ct);
        if (profile is { IsSuccess: true, Data: { } p })
        {
            ViewBag.Sidebar = new AccountSidebarVm
            {
                AvatarUrl = p.AvatarUrl,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName) ? $"{p.FirstName} {p.LastName}".Trim() : p.DisplayName,
                Email = p.Email
            };
        }
        else
        {
            ViewBag.Sidebar = new AccountSidebarVm();
        }
    }
}
