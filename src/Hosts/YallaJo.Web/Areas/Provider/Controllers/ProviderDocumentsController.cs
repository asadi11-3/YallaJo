using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.ProviderDocuments;
using YallaJo.Web.Areas.Provider.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = "Provider")]
[RequirePermission(WebPermission.ProviderDocument.Read)]
public sealed class ProviderDocumentsController : BaseController
{
    private readonly ProviderDocumentsFacade _facade;

    public ProviderDocumentsController(ProviderDocumentsFacade facade) => _facade = facade;

    // ── GET /provider/documents ───────────────────────────────────────────────────
    [HttpGet("provider/documents")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        SetSidebar();

        var result = await _facade.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ProviderDocumentsIndexVm());
        }

        return View(result.Data);
    }

    // ── POST /provider/documents/upload  (multipart) ──────────────────────────────
    [HttpPost("provider/documents/upload")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderDocument.Create)]
    public async Task<IActionResult> Upload(UploadProviderDocumentFormVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (!ModelState.IsValid)
            return await ReloadAsync(form, ct);

        var result = await _facade.UploadAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
                SetError(result.Error);
            return await ReloadAsync(form, ct);
        }

        SetSuccess("Document uploaded.");
        return RedirectToAction(nameof(Index));
    }

    // ── POST /provider/documents/{id}/replace  (multipart; PUT semantics) ─────────
    [HttpPost("provider/documents/{id:guid}/replace")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderDocument.Update)]
    public async Task<IActionResult> Replace(
        Guid id, IFormFile? file, DateOnly? expiresAt, string? rowVersion, CancellationToken ct = default)
    {
        var result = await _facade.ReplaceAsync(id, file, expiresAt, rowVersion ?? string.Empty, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Document replaced.", "Could not replace the document.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadAsync(UploadProviderDocumentFormVm form, CancellationToken ct)
    {
        var result = await _facade.GetAsync(ct);
        var vm = result is { IsSuccess: true, Data: { } data } ? data : new ProviderDocumentsIndexVm();
        vm.Upload = form;
        return View(nameof(Index), vm);
    }

    private void SetSidebar()
    {
        ViewData["ProviderNav"] = "Documents";
        ViewBag.Sidebar = new ProviderSidebarVm { DisplayName = User.Identity?.Name ?? "Provider" };
    }
}
