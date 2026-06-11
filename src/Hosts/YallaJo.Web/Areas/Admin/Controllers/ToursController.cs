using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Tours;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Tour.ReadAny)]
public sealed class ToursController : BaseController
{
    private const int DefaultPageSize = 20;
    private const string DefaultStatus = "Pending";

    private readonly AdminToursFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ToursController(AdminToursFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    // ── GET /admin/tours ──────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index(
        string? status = DefaultStatus,
        int page = 1,
        string? sort = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var result = await _facade.GetListAsync(status, page, DefaultPageSize, sort, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        // S1/PE1 — same action serves the full page and the listing.js fragment
        // (WantsAjax = X-Requested-With: fetch). No [OutputCache] ever (C2).
        if (!result.IsSuccess || result.Data is null)
        {
            ViewBag.Error = result.Error;
            var fallback = new AdminToursIndexVm { Status = status };
            return WantsAjax() ? PartialView("_ToursResults", fallback) : View(fallback);
        }

        return WantsAjax() ? PartialView("_ToursResults", result.Data) : View(result.Data);
    }

    // ── GET /admin/tours/{id} ───────────────────────────────────────────────────────
    [HttpGet("admin/tours/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var result = await _facade.GetDetailsAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsNotFound) return NotFound();
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ── POST /admin/tours/{id}/approve ──────────────────────────────────────────────
    [HttpPost("admin/tours/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Approve)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await _facade.ApproveAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Shared.Flash.TourApproved"].Value, _localizer["Admin.Shared.Flash.TourApproveFailed"].Value);
        return BackToDetails(id);
    }

    // ── POST /admin/tours/{id}/reject ───────────────────────────────────────────────
    [HttpPost("admin/tours/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Reject)]
    public async Task<IActionResult> Reject(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.RejectReasonRequired"].Value);
            return BackToDetails(id);
        }

        var result = await _facade.RejectAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Shared.Flash.TourRejected"].Value, _localizer["Admin.Shared.Flash.TourRejectFailed"].Value);
        return BackToDetails(id);
    }

    // ── POST /admin/tours/{id}/suspend ──────────────────────────────────────────────
    [HttpPost("admin/tours/{id:guid}/suspend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Suspend)]
    public async Task<IActionResult> Suspend(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.SuspendReasonRequired"].Value);
            return BackToDetails(id);
        }

        var result = await _facade.SuspendAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Shared.Flash.TourSuspended"].Value, _localizer["Admin.Shared.Flash.TourSuspendFailed"].Value);
        return BackToDetails(id);
    }

    // ── POST /admin/tours/{id}/reinstate ────────────────────────────────────────────
    [HttpPost("admin/tours/{id:guid}/reinstate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Reinstate)]
    public async Task<IActionResult> Reinstate(Guid id, CancellationToken ct)
    {
        var result = await _facade.ReinstateAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Shared.Flash.TourReinstated"].Value, _localizer["Admin.Shared.Flash.TourReinstateFailed"].Value);
        return BackToDetails(id);
    }

    // ── POST /admin/tours/{id}/feature ──────────────────────────────────────────────
    [HttpPost("admin/tours/{id:guid}/feature")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Feature)]
    public async Task<IActionResult> Feature(Guid id, bool isFeatured, CancellationToken ct)
    {
        var result = await _facade.FeatureAsync(id, isFeatured, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(
            result,
            isFeatured ? _localizer["Admin.Tours.Flash.Featured"].Value : _localizer["Admin.Tours.Flash.Unfeatured"].Value,
            isFeatured ? _localizer["Admin.Tours.Flash.FeatureFailed"].Value : _localizer["Admin.Tours.Flash.UnfeatureFailed"].Value);
        return BackToDetails(id);
    }

    // ── POST /admin/tours/proposals/{id}/approve ────────────────────────────────────
    [HttpPost("admin/tours/proposals/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TourProposal.Approve)]
    public async Task<IActionResult> ApproveProposal(Guid id, bool isExclusive, CancellationToken ct)
    {
        var result = await _facade.ApproveProposalAsync(id, isExclusive, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Tours.Flash.ProposalApproved"].Value, _localizer["Admin.Tours.Flash.ProposalApproveFailed"].Value);
        return RedirectToAction(nameof(Index), new { status = DefaultStatus });
    }

    // ── POST /admin/tours/proposals/{id}/reject ─────────────────────────────────────
    [HttpPost("admin/tours/proposals/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.TourProposal.Reject)]
    public async Task<IActionResult> RejectProposal(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.RejectReasonRequired"].Value);
            return RedirectToAction(nameof(Index), new { status = DefaultStatus });
        }

        var result = await _facade.RejectProposalAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Tours.Flash.ProposalRejected"].Value, _localizer["Admin.Tours.Flash.ProposalRejectFailed"].Value);
        return RedirectToAction(nameof(Index), new { status = DefaultStatus });
    }

    // ── POST /admin/tours/packages/{id}/approve ─────────────────────────────────────
    [HttpPost("admin/tours/packages/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Package.Approve)]
    public async Task<IActionResult> ApprovePackage(Guid id, Guid? tourId, CancellationToken ct)
    {
        var result = await _facade.ApprovePackageAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Tours.Flash.PackageApproved"].Value, _localizer["Admin.Tours.Flash.PackageApproveFailed"].Value);
        return tourId is { } tid
            ? BackToDetails(tid)
            : RedirectToAction(nameof(Index), new { status = DefaultStatus });
    }

    // ── POST /admin/tours/packages/{id}/reject ──────────────────────────────────────
    [HttpPost("admin/tours/packages/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Package.Reject)]
    public async Task<IActionResult> RejectPackage(Guid id, string? reason, Guid? tourId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.RejectReasonRequired"].Value);
            return tourId is { } tidErr
                ? BackToDetails(tidErr)
                : RedirectToAction(nameof(Index), new { status = DefaultStatus });
        }

        var result = await _facade.RejectPackageAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Tours.Flash.PackageRejected"].Value, _localizer["Admin.Tours.Flash.PackageRejectFailed"].Value);
        return tourId is { } tid
            ? BackToDetails(tid)
            : RedirectToAction(nameof(Index), new { status = DefaultStatus });
    }

    // ── POST /admin/tours/{tourId}/guide-offerings/{guideId}/suspend ─────────────────
    [HttpPost("admin/tours/{tourId:guid}/guide-offerings/{guideId:guid}/suspend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.GuideOffering.Suspend)]
    public async Task<IActionResult> SuspendOffering(Guid tourId, Guid guideId, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.SuspendReasonRequired"].Value);
            return BackToDetails(tourId);
        }

        var result = await _facade.SuspendOfferingAsync(tourId, guideId, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Tours.Flash.OfferingSuspended"].Value, _localizer["Admin.Tours.Flash.OfferingSuspendFailed"].Value);
        return BackToDetails(tourId);
    }

    // ── POST /admin/tours/{tourId}/guide-offerings/{guideId}/reinstate ───────────────
    [HttpPost("admin/tours/{tourId:guid}/guide-offerings/{guideId:guid}/reinstate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.GuideOffering.Reinstate)]
    public async Task<IActionResult> ReinstateOffering(Guid tourId, Guid guideId, CancellationToken ct)
    {
        var result = await _facade.ReinstateOfferingAsync(tourId, guideId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Tours.Flash.OfferingReinstated"].Value, _localizer["Admin.Tours.Flash.OfferingReinstateFailed"].Value);
        return BackToDetails(tourId);
    }

    // PRG target shared by all moderation/curation actions — redirect back to the
    // tour Details page. Pure refactor of the repeated RedirectToAction(nameof(Details), …).
    private IActionResult BackToDetails(Guid id) =>
        RedirectToAction(nameof(Details), new { id });
}
