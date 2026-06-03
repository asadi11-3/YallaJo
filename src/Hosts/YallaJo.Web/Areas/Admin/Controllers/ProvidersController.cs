using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin provider-application review queue (<c>/admin/providers</c>).
/// All data comes from the queue summary endpoint (no admin detail endpoint exists),
/// so moderation actions are invoked from the queue rows and redirect back to the list.
/// </summary>
[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminProviderQueue.Read)]
public sealed class ProvidersController : BaseController
{
    private const int DefaultPageSize = 20;

    private readonly ProvidersFacade _facade;

    public ProvidersController(ProvidersFacade facade) => _facade = facade;

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

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(Models.Providers.ProvidersMapper.EmptyQueue(status, type, page, DefaultPageSize));
        }

        return View(result.Data);
    }

    // ── POST /admin/providers/{id}/approve ──────────────────────────────────────────
    [HttpPost("admin/providers/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminProviderQueue.Approve)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var result = await _facade.ApproveAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Provider application approved.", "Could not approve the application.");
        return BackToQueue();
    }

    // ── POST /admin/providers/{id}/reject ───────────────────────────────────────────
    [HttpPost("admin/providers/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminProviderQueue.Reject)]
    public async Task<IActionResult> Reject(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A rejection reason is required.");
            return BackToQueue();
        }

        var result = await _facade.RejectAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Provider application rejected.", "Could not reject the application.");
        return BackToQueue();
    }

    // ── POST /admin/providers/{id}/request-documents ────────────────────────────────
    [HttpPost("admin/providers/{id:guid}/request-documents")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminProviderQueue.RequestDocs)]
    public async Task<IActionResult> RequestDocuments(
        Guid id, string[]? missingDocumentTypes, string? notes, CancellationToken ct)
    {
        var docs = (missingDocumentTypes ?? [])
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim())
            .ToList();

        if (docs.Count == 0)
        {
            SetError("Select at least one missing document type.");
            return BackToQueue();
        }

        if (string.IsNullOrWhiteSpace(notes))
        {
            SetError("Notes are required when requesting documents.");
            return BackToQueue();
        }

        var result = await _facade.RequestDocsAsync(id, docs, notes.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Documents requested from the provider.", "Could not request documents.");
        return BackToQueue();
    }

    // ── POST /admin/providers/{id}/suspend ──────────────────────────────────────────
    [HttpPost("admin/providers/{id:guid}/suspend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminProviderQueue.Suspend)]
    public async Task<IActionResult> Suspend(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A suspension reason is required.");
            return BackToQueue();
        }

        var result = await _facade.SuspendAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Provider suspended.", "Could not suspend the provider.");
        return BackToQueue();
    }

    // ── POST /admin/providers/{id}/reinstate ────────────────────────────────────────
    [HttpPost("admin/providers/{id:guid}/reinstate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminProviderQueue.Reinstate)]
    public async Task<IActionResult> Reinstate(Guid id, CancellationToken ct)
    {
        var result = await _facade.ReinstateAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Provider reinstated.", "Could not reinstate the provider.");
        return BackToQueue();
    }

    private IActionResult BackToQueue() => RedirectToAction(nameof(Index));
}
