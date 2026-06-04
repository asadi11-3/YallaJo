using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Tours;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Tour.ReadAny)]
public sealed class ToursController : BaseController
{
    private const int DefaultPageSize = 20;
    private const string DefaultStatus = "Pending";

    private readonly AdminToursFacade _facade;

    public ToursController(AdminToursFacade facade) => _facade = facade;

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

        if (!result.IsSuccess || result.Data is null)
        {
            ViewBag.Error = result.Error;
            return View(new AdminToursIndexVm { Status = status });
        }

        return View(result.Data);
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

        SetFlash(result, "Tour approved.", "Could not approve the tour.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── POST /admin/tours/{id}/reject ───────────────────────────────────────────────
    [HttpPost("admin/tours/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Reject)]
    public async Task<IActionResult> Reject(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A rejection reason is required.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _facade.RejectAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Tour rejected.", "Could not reject the tour.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── POST /admin/tours/{id}/suspend ──────────────────────────────────────────────
    [HttpPost("admin/tours/{id:guid}/suspend")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Suspend)]
    public async Task<IActionResult> Suspend(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A suspension reason is required.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _facade.SuspendAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Tour suspended.", "Could not suspend the tour.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── POST /admin/tours/{id}/reinstate ────────────────────────────────────────────
    [HttpPost("admin/tours/{id:guid}/reinstate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tour.Reinstate)]
    public async Task<IActionResult> Reinstate(Guid id, CancellationToken ct)
    {
        var result = await _facade.ReinstateAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Tour reinstated.", "Could not reinstate the tour.");
        return RedirectToAction(nameof(Details), new { id });
    }
}
