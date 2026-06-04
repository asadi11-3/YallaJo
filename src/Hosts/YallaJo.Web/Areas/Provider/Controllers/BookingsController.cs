using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Bookings;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class BookingsController : BaseController
{
    private const string DefaultStatus = "PendingConfirmation";

    private readonly ProviderBookingsFacade _facade;
    private readonly ICurrentUser _currentUser;

    public BookingsController(ProviderBookingsFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // ── GET /provider/bookings ────────────────────────────────────────────────────
    [HttpGet("provider/bookings")]
    public async Task<IActionResult> Index(string? status = null, CancellationToken ct = default)
    {
        if (!_currentUser.HasPermission(WebPermission.TourBooking.ReadOwn))
            return RedirectToStatus();

        var result = await _facade.GetListAsync(status, ct);
        return result.Outcome switch
        {
            ProviderBookingOutcome.Ok => View(result.Data),
            ProviderBookingOutcome.ForceSignOut => RedirectToLogin(),
            ProviderBookingOutcome.Forbidden => Denied(result.Error),
            _ => Failed(result.Error),
        };
    }

    // ── GET /provider/bookings/{id} ───────────────────────────────────────────────
    [HttpGet("provider/bookings/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourBooking.ReadOwn))
            return RedirectToStatus();

        var result = await _facade.GetDetailsAsync(id, ct);
        return result.Outcome switch
        {
            ProviderBookingOutcome.Ok => View(result.Data),
            ProviderBookingOutcome.ForceSignOut => RedirectToLogin(),
            ProviderBookingOutcome.Forbidden => Denied(result.Error),
            ProviderBookingOutcome.NotFound => NotFoundRedirect(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── POST /provider/bookings/{id}/confirm ──────────────────────────────────────
    [HttpPost("provider/bookings/{id:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourBooking.Confirm))
            return RedirectToStatus();

        var result = await _facade.ConfirmAsync(id, ct);
        if (result.Outcome == ProviderBookingOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == ProviderBookingOutcome.Ok)
            SetSuccess("Booking confirmed.");
        else
            SetError(result.Error ?? "Could not confirm the booking.");

        return RedirectToAction(nameof(Details), new { id });
    }

    // ── POST /provider/bookings/{id}/cancel ───────────────────────────────────────
    [HttpPost("provider/bookings/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, ProviderBookingCancelVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.TourBooking.Cancel))
            return RedirectToStatus();

        // Guard the reason before the API call (provider cancel requires reason >= 10).
        var reason = vm.Reason?.Trim() ?? string.Empty;
        if (reason.Length < 10)
        {
            SetError("A cancellation reason of at least 10 characters is required.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _facade.CancelAsync(id, reason, ct);
        if (result.Outcome == ProviderBookingOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == ProviderBookingOutcome.Ok)
            SetSuccess("Booking cancelled. A full refund will be processed.");
        else
            SetError(result.Error ?? "Could not cancel the booking.");

        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private IActionResult Denied(string? message)
    {
        SetError(message ?? "You don't have access to this booking.");
        return RedirectToAction(nameof(Index));
    }

    private IActionResult NotFoundRedirect(string? message)
    {
        SetError(message ?? "Booking not found.");
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Failed(string? message)
    {
        ViewBag.Error = message;
        return View(nameof(Index), new ProviderBookingsIndexVm());
    }

    private IActionResult RedirectToStatus() =>
        RedirectToAction("Status", "Provider", new { area = "Provider" });
}
