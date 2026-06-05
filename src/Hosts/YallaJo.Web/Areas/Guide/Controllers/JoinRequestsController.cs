using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.JoinRequests;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

[Area("Guide")]
[Authorize]
public sealed class JoinRequestsController : BaseController
{
    private readonly GuideJoinRequestsFacade _joinRequests;

    public JoinRequestsController(GuideJoinRequestsFacade joinRequests) => _joinRequests = joinRequests;

    [HttpGet("guide/join-requests")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();
        var result = await _joinRequests.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new JoinRequestsVm());
        }

        return View(result.Data);
    }

    [HttpPost("guide/join-requests/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(RespondToJoinRequestFormVm form, CancellationToken ct = default)
    {
        var result = await _joinRequests.ApproveAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Join request approved.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/join-requests/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RespondToJoinRequestFormVm form, CancellationToken ct = default)
    {
        var result = await _joinRequests.RejectAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        SetFlash(result, "Join request rejected.");
        return RedirectToAction(nameof(Index));
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "JoinRequests";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
