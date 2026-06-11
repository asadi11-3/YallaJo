using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.AgencyRoster;
using YallaJo.Web.Areas.Guide.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Guide.Controllers;

/// <summary>
/// Agency-OWNER roster management at <c>/guide/agency/roster</c>. Separate from the
/// guide self-service <see cref="AgencyController"/> (/guide/agency). Gated by the
/// AgencyRoster permission; the backend additionally enforces agency-ownership.
/// </summary>
[Area("Guide")]
[Authorize]
[RequirePermission(WebPermission.AgencyRoster.Read)]
public sealed class AgencyRosterController : BaseController
{
    private readonly GuideAgencyRosterFacade _facade;

    public AgencyRosterController(GuideAgencyRosterFacade facade) => _facade = facade;

    [HttpGet("guide/agency/roster")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();
        var result = await _facade.GetRosterAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new AgencyRosterVm());
        }

        return View(result.Data);
    }

    [HttpGet("guide/agency/roster/invite")]
    [RequirePermission(WebPermission.AgencyRoster.Create)]
    public async Task<IActionResult> Invite(CancellationToken ct = default)
    {
        SetSidebar();
        var result = await _facade.GetInviteFormAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("guide/agency/roster/invite")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AgencyRoster.Create)]
    public async Task<IActionResult> Invite(InviteGuideFormVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (!ModelState.IsValid)
        {
            var repopulate = await _facade.PopulateAvailableGuidesAsync(form, ct);
            if (GuardSignOut(repopulate) is { } repopulateSignOut) return repopulateSignOut;
            return View(form);
        }

        var result = await _facade.InviteAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            var repopulate = await _facade.PopulateAvailableGuidesAsync(form, ct);
            if (GuardSignOut(repopulate) is { } repopulateSignOut) return repopulateSignOut;
            return View(form);
        }

        SetSuccess("Invitation sent.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/agency/roster/applications/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AgencyRoster.Approve)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.ApproveApplicationAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Application approved. The guide has been added to your roster.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/agency/roster/applications/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AgencyRoster.Reject)]
    public async Task<IActionResult> Reject(Guid id, string? reason, CancellationToken ct = default)
    {
        if (RequireReason(reason, "Please provide a reason for rejecting the application.") is { } invalid)
        {
            return invalid;
        }

        var result = await _facade.RejectApplicationAsync(id, reason!, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Application rejected.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("guide/agency/roster/guides/{guideUserId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AgencyRoster.Delete)]
    public async Task<IActionResult> Remove(Guid guideUserId, string? reason, CancellationToken ct = default)
    {
        if (RequireReason(reason, "Please provide a reason for removing the guide.") is { } invalid)
        {
            return invalid;
        }

        var result = await _facade.RemoveGuideAsync(guideUserId, reason!, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Guide removed from your roster.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Shared reason-required guard for reject/remove actions; null when the reason is present.</summary>
    private IActionResult? RequireReason(string? reason, string errorMessage)
    {
        if (!string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        SetError(errorMessage);
        return RedirectToAction(nameof(Index));
    }

    private void SetSidebar()
    {
        ViewData["GuideNav"] = "AgencyRoster";
        ViewBag.Sidebar = new GuideSidebarVm { DisplayName = User.Identity?.Name ?? "Guide" };
    }
}
