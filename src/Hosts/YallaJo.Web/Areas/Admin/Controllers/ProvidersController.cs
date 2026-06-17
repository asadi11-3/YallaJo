using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin provider-application review queue (<c>/admin/providers</c>) and details page
/// (<c>/admin/providers/{id}</c>). Moderation actions can be invoked from either the
/// queue rows or the details page; <c>fromDetails</c> controls which page they redirect
/// back to so the caller sees the updated status in place.
/// </summary>
[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminProviderQueue.Read)]
public sealed class ProvidersController : BaseController
{
    private const int DefaultPageSize = 20;

    private readonly ProvidersFacade _facade;
    private readonly ProviderPaymentMethodsFacade _paymentMethods;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ProvidersController(ProvidersFacade facade, ProviderPaymentMethodsFacade paymentMethods, IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
        _facade = facade;
        _paymentMethods = paymentMethods;
    }

    // ── GET /admin/providers ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index(
        string? status = null,
        string? type = null,
        int page = 1,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var result = await _facade.GetQueueAsync(status, type, page, DefaultPageSize, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        // S1/PE1 — same action serves the full page and the listing.js fragment
        // (WantsAjax = X-Requested-With: fetch). No [OutputCache] ever (C2).
        if (!result.IsSuccess || result.Data is null)
        {
            var fallback = Models.Providers.ProvidersMapper.EmptyQueue(status, type, page, DefaultPageSize);
            if (WantsAjax())
            {
                ViewBag.Error = result.Error;
                return PartialView("_ProvidersResults", fallback);
            }

            SetError(result.Error);
            return View(fallback);
        }

        return WantsAjax() ? PartialView("_ProvidersResults", result.Data) : View(result.Data);
    }

    // ── GET /admin/providers/{id} ─────────────────────────────────────────────────────
    [HttpGet("admin/providers/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var result = await _facade.GetDetailsAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ── GET /admin/providers/documents/{documentId}/download ──────────────────────────
    // Proxies the authorized API download endpoint so reviewers can open a provider's
    // document without exposing the on-disk URL/StorageKey. The API enforces owner/admin-tier
    // access; nothing here logs or returns a physical path.
    [HttpGet("admin/providers/documents/{documentId:guid}/download")]
    [RequirePermission(WebPermission.AdminProviderQueue.Read)]
    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken ct)
    {
        var result = await _facade.DownloadDocumentAsync(documentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? _localizer["Admin.ProviderDetails.DownloadFailed"].Value);
            return RedirectToAction(nameof(Index));
        }

        var file = result.Data;
        return File(file.Content, file.ContentType, file.FileName);
    }

    // ── POST /admin/providers/{id}/approve ──────────────────────────────────────────
    [HttpPost("admin/providers/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminProviderQueue.Approve)]
    public async Task<IActionResult> Approve(Guid id, bool fromDetails, CancellationToken ct)
    {
        var result = await _facade.ApproveAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Providers.Flash.Approved"].Value, _localizer["Admin.Providers.Flash.ApproveFailed"].Value);
        return Back(id, fromDetails);
    }

    // ── POST /admin/providers/{id}/reject ───────────────────────────────────────────
    [HttpPost("admin/providers/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminProviderQueue.Reject)]
    public async Task<IActionResult> Reject(Guid id, string? reason, bool fromDetails, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.RejectReasonRequired"].Value);
            return Back(id, fromDetails);
        }

        var result = await _facade.RejectAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Providers.Flash.Rejected"].Value, _localizer["Admin.Providers.Flash.RejectFailed"].Value);
        return Back(id, fromDetails);
    }

    // ── POST /admin/providers/{id}/request-documents ────────────────────────────────
    [HttpPost("admin/providers/{id:guid}/request-documents")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminProviderQueue.RequestDocs)]
    public async Task<IActionResult> RequestDocuments(
        Guid id, string[]? missingDocumentTypes, string? notes, bool fromDetails, CancellationToken ct)
    {
        var docs = (missingDocumentTypes ?? [])
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim())
            .ToList();

        if (docs.Count == 0)
        {
            SetError(_localizer["Admin.Providers.Flash.DocTypesRequired"].Value);
            return Back(id, fromDetails);
        }

        if (string.IsNullOrWhiteSpace(notes))
        {
            SetError(_localizer["Admin.Providers.Flash.DocNotesRequired"].Value);
            return Back(id, fromDetails);
        }

        var result = await _facade.RequestDocsAsync(id, docs, notes.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Providers.Flash.DocsRequested"].Value, _localizer["Admin.Providers.Flash.DocsRequestFailed"].Value);
        return Back(id, fromDetails);
    }

    // ── POST /admin/providers/{id}/suspend ──────────────────────────────────────────
    [HttpPost("admin/providers/{id:guid}/suspend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminProviderQueue.Suspend)]
    public async Task<IActionResult> Suspend(Guid id, string? reason, bool fromDetails, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError(_localizer["Admin.Shared.Flash.SuspendReasonRequired"].Value);
            return Back(id, fromDetails);
        }

        var result = await _facade.SuspendAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Providers.Flash.Suspended"].Value, _localizer["Admin.Providers.Flash.SuspendFailed"].Value);
        return Back(id, fromDetails);
    }

    // ── POST /admin/providers/{id}/reinstate ────────────────────────────────────────
    [HttpPost("admin/providers/{id:guid}/reinstate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminProviderQueue.Reinstate)]
    public async Task<IActionResult> Reinstate(Guid id, bool fromDetails, CancellationToken ct)
    {
        var result = await _facade.ReinstateAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Providers.Flash.Reinstated"].Value, _localizer["Admin.Providers.Flash.ReinstateFailed"].Value);
        return Back(id, fromDetails);
    }

    // ── POST /admin/providers/{id}/payment-methods/{methodId}/verify ─────────────────
    // §8.7 — admin/KYC: confirm or revoke verification of a provider payout method.
    // Gated separately by the Finance-mirrored ProviderPaymentMethod.Verify permission
    // (the page-level attribute only grants AdminProviderQueue.Read). `id` is the
    // provider id, used only to redirect back to the right details page.
    [HttpPost("admin/providers/{id:guid}/payment-methods/{methodId:guid}/verify")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderPaymentMethod.Verify)]
    public async Task<IActionResult> VerifyPaymentMethod(Guid id, Guid methodId, bool isVerified, CancellationToken ct)
    {
        var result = await _paymentMethods.VerifyAsync(methodId, isVerified, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(
            result,
            isVerified ? _localizer["Admin.Providers.Flash.PaymentVerified"].Value : _localizer["Admin.Providers.Flash.PaymentUnverified"].Value,
            _localizer["Admin.Providers.Flash.PaymentVerifyFailed"].Value);
        return RedirectToAction(nameof(Details), new { id });
    }

    // Redirects to the details page when the action came from there (so the admin sees
    // the updated status in place); otherwise back to the queue (preserves prior behavior).
    private IActionResult Back(Guid id, bool fromDetails) =>
        fromDetails
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Index));
}
