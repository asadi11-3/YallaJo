using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

/// <summary>
/// Canonical provider booking-management surface: an owner-scoped, paged booking
/// list and a per-booking details page with the provider state-machine actions
/// (confirm / reject / cancel / complete). The existing <see cref="BookingsController"/>
/// remains the join-request queue + single-booking lookup hub at <c>/provider/bookings</c>.
/// </summary>
[Area("Provider")]
[Authorize]
public sealed class ProviderBookingsController : BaseController
{
    private readonly ProviderBookingsFacade _facade;

    public ProviderBookingsController(ProviderBookingsFacade facade) => _facade = facade;

    [HttpGet("provider/bookings/manage")]
    public async Task<IActionResult> Index(string? status, CancellationToken ct = default)
    {
        SetSidebar();
        var result = await _facade.GetListAsync(status, ct);
        if (result.Outcome == ProviderBookingOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome != ProviderBookingOutcome.Ok || result.Data is null)
        {
            SetError(result.Error);
            return View(new Models.Bookings.ProviderBookingsIndexVm { Status = status });
        }

        return View(result.Data);
    }

    [HttpGet("provider/bookings/manage/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
    {
        SetSidebar();
        var result = await _facade.GetDetailsAsync(id, ct);
        if (result.Outcome == ProviderBookingOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == ProviderBookingOutcome.NotFound) return NotFound();

        if (result.Outcome != ProviderBookingOutcome.Ok || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("provider/bookings/manage/{id:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.ConfirmAsync(id, ct);
        return Finish(result, id, "Booking confirmed.");
    }

    [HttpPost("provider/bookings/manage/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string? reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("Please provide a reason for rejecting the booking.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _facade.RejectAsync(id, reason, ct);
        return Finish(result, id, "Booking rejected.");
    }

    [HttpPost("provider/bookings/manage/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string? reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("Please provide a reason for cancelling the booking.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _facade.CancelAsync(id, reason, ct);
        return Finish(result, id, "Booking cancelled.");
    }

    [HttpPost("provider/bookings/manage/{id:guid}/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.CompleteAsync(id, ct);
        return Finish(result, id, "Booking marked as completed.");
    }

    private IActionResult Finish(ProviderBookingActionResult result, Guid id, string successMessage)
    {
        if (result.Outcome == ProviderBookingOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == ProviderBookingOutcome.Ok)
            SetSuccess(successMessage);
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Details), new { id });
    }

    private void SetSidebar()
    {
        ViewData["ProviderNav"] = "Bookings";
        ViewBag.Sidebar = new ProviderSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Provider",
        };
    }
}
