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
    private readonly BookingsFacade _facade;

    public BookingsController(BookingsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] BookingsFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Bookings";

        var result = await _facade.GetIndexAsync(request, ct);

        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new BookingsVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/bookings/{id:guid}/force-refund")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.AdminBookingDashboard.Update)]
    public async Task<IActionResult> ForceRefund(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A refund reason is required.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.ForceRefundAsync(id, reason.Trim(), ct);

        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Booking force-refunded.", "Could not force-refund the booking.");
        return RedirectToAction(nameof(Index));
    }
}
