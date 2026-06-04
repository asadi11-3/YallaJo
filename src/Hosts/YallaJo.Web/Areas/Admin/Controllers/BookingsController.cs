using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Bookings;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminBookingDashboard.Read)]
public sealed class BookingsController : BaseController
{
    private const int DefaultPageSize = 20;

    private readonly AdminBookingsFacade _facade;

    public BookingsController(AdminBookingsFacade facade) => _facade = facade;

    // ── GET /admin/bookings ─────────────────────────────────────────────────────────
    [HttpGet("admin/bookings")]
    public async Task<IActionResult> Index(
        string? status = null,
        string? fromDate = null,
        string? toDate = null,
        string? providerId = null,
        string? tourId = null,
        string? userId = null,
        string? cursor = null,
        CancellationToken ct = default)
    {
        var filters = new AdminBookingFiltersVm
        {
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            ProviderId = providerId,
            TourId = tourId,
            UserId = userId,
        };

        var result = await _facade.GetListAsync(filters, cursor, DefaultPageSize, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            ViewBag.Error = result.Error;
            return View(new AdminBookingsIndexVm { Filters = filters });
        }

        return View(result.Data);
    }

    // ── GET /admin/bookings/{id} ──────────────────────────────────────────────────────
    [HttpGet("admin/bookings/{id:guid}")]
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
}
