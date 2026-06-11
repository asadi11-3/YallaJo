using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.AuditLogs;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.System.Read)]
public sealed class AuditLogsController : BaseController
{
    private readonly AuditLogsFacade _facade;
    public AuditLogsController(AuditLogsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        Guid? userId = null,
        Guid? actorUserId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await _facade.GetLogsAsync(
            page,
            pageSize: 50,
            userId: userId,
            actorUserId: actorUserId,
            action: action,
            from: from,
            to: to,
            ct: ct);

        if (GuardSignOut(result) is { } signOut) return signOut;

        // S1/PE1 — the same action serves the full page and the listing.js
        // fragment (WantsAjax = X-Requested-With: fetch). No [OutputCache] ever (C2).
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            // Preserve whatever filters the caller supplied so the view
            // can re-render the filter bar correctly even on error.
            var fallback = new AuditLogListVm
            {
                FilterUserId      = userId,
                FilterActorUserId = actorUserId,
                FilterAction      = action,
                FilterFrom        = from,
                FilterTo          = to,
            };
            return WantsAjax() ? PartialView("_AuditLogsResults", fallback) : View(fallback);
        }

        return WantsAjax() ? PartialView("_AuditLogsResults", result.Data) : View(result.Data);
    }

    // ── POST /admin/audit-logs/{id}/redact ──────────────────────────────────────────
    // §8.13 — financial/compliance action; gated separately from the page-level read.
    [HttpPost("admin/audit-logs/{id:long}/redact")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AuditLog.Redact)]
    public async Task<IActionResult> Redact(long id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A redaction reason is required.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.RedactAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Audit-log entry redacted.", "Could not redact the audit-log entry.");
        return RedirectToAction(nameof(Index));
    }

    // ── GET /admin/audit-logs/export ────────────────────────────────────────────────
    // §8.13 — exports the selected date range as a downloadable CSV.
    [HttpGet("admin/audit-logs/export")]
    [RequirePermission(WebPermission.AuditLog.Export)]
    public async Task<IActionResult> Export(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var rangeFrom = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var rangeTo   = to ?? DateTime.UtcNow;

        var result = await _facade.ExportAsync(rangeFrom, rangeTo, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? "Could not export the audit logs.");
            return RedirectToAction(nameof(Index));
        }

        var fileName = $"audit-logs_{rangeFrom:yyyyMMdd}_{rangeTo:yyyyMMdd}.csv";
        return File(result.Data.Content, result.Data.ContentType, fileName);
    }
}
