using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

/// <summary>
/// The Provider Dashboard "Documents" tab. Shows the provider's own
/// application/legal documents (Accounts module, keyed by the caller's
/// ProviderApplication) — NOT tour-guide booking documents. List/upload/replace/
/// download are all served by the Accounts <c>/api/v1/provider/*</c> endpoints,
/// so a non-guide provider no longer hits the Booking "not registered as a tour
/// guide" error.
/// </summary>
[Area("Provider")]
[Authorize]
[RequirePermission(WebPermission.ProviderApplication.Read)]
public sealed class ProviderDocumentsController : BaseController
{
    private readonly ProviderFacade _facade;

    public ProviderDocumentsController(ProviderFacade facade) => _facade = facade;

    // ── GET /provider/documents ───────────────────────────────────────────────────
    [HttpGet("provider/documents")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var result = await _facade.GetStatusAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ProviderStatusVm { HasApplication = false });
        }

        return View(result.Data);
    }

    // ── POST /provider/documents/upload  (multipart) ──────────────────────────────
    [HttpPost("provider/documents/upload")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderApplication.Create)]
    public async Task<IActionResult> Upload(ProviderDocumentUploadVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
            SetError(firstError ?? L["Provider.Flash.FixUploadForm"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.UploadDocumentAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsValidationError)
            SetError(result.ValidationErrors!.SelectMany(kvp => kvp.Value).FirstOrDefault()
                     ?? L["Provider.Flash.DocumentRejected"].Value);
        else
            SetFlash(result, L["Provider.Flash.DocumentUploaded"], L["Provider.Flash.CouldNotUploadDocument"].Value);

        return RedirectToAction(nameof(Index));
    }

    // ── POST /provider/documents/{id}/replace  (multipart; PUT semantics) ─────────
    [HttpPost("provider/documents/{id:guid}/replace")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderApplication.Update)]
    public async Task<IActionResult> Replace(Guid id, ReplaceProviderDocumentVm form, CancellationToken ct = default)
    {
        // The route id is authoritative for which document is being replaced.
        form.DocumentId = id;

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
            SetError(firstError ?? L["Provider.Flash.FixReplaceForm"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.ReplaceDocumentAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsValidationError)
            SetError(result.ValidationErrors!.SelectMany(kvp => kvp.Value).FirstOrDefault()
                     ?? L["Provider.Flash.ReplacementRejected"].Value);
        else
            SetFlash(result, L["Provider.Flash.DocumentReplaced"], L["Provider.Flash.CouldNotReplaceDocument"].Value);

        return RedirectToAction(nameof(Index));
    }

    // ── GET /provider/documents/{id}/download ─────────────────────────────────────
    // Proxies the authorized Accounts API download endpoint so providers can open
    // their own document without exposing the on-disk URL/StorageKey. The API
    // enforces owner/admin access; a missing or foreign document returns 404 there.
    [HttpGet("provider/documents/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.DownloadDocumentAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? L["Provider.Flash.CouldNotDownloadDocument"].Value);
            return RedirectToAction(nameof(Index));
        }

        var file = result.Data;
        return File(file.Content, file.ContentType, file.FileName);
    }
}
