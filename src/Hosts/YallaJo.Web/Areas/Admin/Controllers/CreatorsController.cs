using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Creators;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminCreatorQueue.Read)]
public sealed class CreatorsController : BaseController
{
    private readonly CreatorsFacade _facade;

    public CreatorsController(CreatorsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(string? status = null, Guid? id = null, int page = 1, CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        ViewData["AdminNav"] = "Creators";

        var result = await _facade.GetIndexAsync(status, id, page, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new CreatorsVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/creators/applications/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminCreatorQueue.Approve)]
    public async Task<IActionResult> Approve(Guid id, string displayName, string? avatarUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            SetError("A display name is required to approve a creator.");
            return Back(id);
        }

        var result = await _facade.ApproveAsync(id, displayName.Trim(), string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Creator application approved.", "Could not approve the creator application.");
        return Back(id);
    }

    [HttpPost("admin/creators/applications/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminCreatorQueue.Reject)]
    public async Task<IActionResult> Reject(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A rejection reason is required.");
            return Back(id);
        }

        var result = await _facade.RejectAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Creator application rejected.", "Could not reject the creator application.");
        return Back(id);
    }

    [HttpPost("admin/creators/applications/{id:guid}/request-more-info")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminCreatorQueue.RequestMoreInfo)]
    public async Task<IActionResult> RequestMoreInfo(Guid id, string? adminNote, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(adminNote))
        {
            SetError("A note describing the requested information is required.");
            return Back(id);
        }

        var result = await _facade.RequestMoreInfoAsync(id, adminNote.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Requested more information from the applicant.", "Could not request more information.");
        return Back(id);
    }

    [HttpPost("admin/creators/profiles/{id:guid}/suspend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminCreatorQueue.Suspend)]
    public async Task<IActionResult> Suspend(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A suspension reason is required.");
            return Back(id);
        }

        var result = await _facade.SuspendAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Creator profile suspended.", "Could not suspend the creator profile.");
        return Back(id);
    }

    [HttpPost("admin/creators/profiles/{id:guid}/reinstate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminCreatorQueue.Reinstate)]
    public async Task<IActionResult> Reinstate(Guid id, CancellationToken ct)
    {
        var result = await _facade.ReinstateAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Creator profile reinstated.", "Could not reinstate the creator profile.");
        return Back(id);
    }

    // ── §8.6: promote / demote tier ─────────────────────────────────────────────
    [HttpPost("admin/creators/profiles/{id:guid}/promote")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminCreatorQueue.PromoteTier)]
    public async Task<IActionResult> Promote(Guid id, string? targetTier, CancellationToken ct)
    {
        var result = await _facade.PromoteAsync(id, targetTier, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Creator promoted.", "Could not promote the creator.");
        return Back(id);
    }

    [HttpPost("admin/creators/profiles/{id:guid}/demote")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminCreatorQueue.DemoteTier)]
    public async Task<IActionResult> Demote(Guid id, string? targetTier, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A demotion reason is required.");
            return Back(id);
        }

        var result = await _facade.DemoteAsync(id, targetTier, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Creator demoted.", "Could not demote the creator.");
        return Back(id);
    }

    // ── §8.6: edit creator profile ──────────────────────────────────────────────
    [HttpPost("admin/creators/profiles/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminCreatorQueue.Update)]
    public async Task<IActionResult> Edit(
        Guid id, string? displayName, string? bio, string? avatarUrl, string? slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            SetError("A display name is required.");
            return Back(id);
        }

        var result = await _facade.EditAsync(id, displayName.Trim(), bio, avatarUrl, slug, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Creator profile updated.", "Could not update the creator profile.");
        return Back(id);
    }

    // ── §8.6: delete (soft) creator profile ─────────────────────────────────────
    [HttpPost("admin/creators/profiles/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminCreatorQueue.Delete)]
    public async Task<IActionResult> Delete(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A reason is required to delete a creator profile.");
            return Back(id);
        }

        var result = await _facade.DeleteAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Creator profile deleted.");
            return RedirectToAction(nameof(Index));
        }

        SetError(result.Error ?? "Could not delete the creator profile.");
        return Back(id);
    }

    // ── §8.6: send creator invitation ───────────────────────────────────────────
    [HttpPost("admin/creators/invitations")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminCreatorQueue.Invite)]
    public async Task<IActionResult> SendInvitation(
        string? kind, string? email, Guid? invitedUserId, string? personalMessage, CancellationToken ct)
    {
        var result = await _facade.SendInvitationAsync(kind, email, invitedUserId, personalMessage, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Creator invitation sent.", "Could not send the creator invitation.");
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Back(Guid id) => RedirectToAction(nameof(Index), new { id });
}
