using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Bookings;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class BookingsController : BaseController
{
    private readonly BookingsFacade _bookings;

    public BookingsController(BookingsFacade bookings) => _bookings = bookings;

    [HttpGet("provider/bookings")]
    public async Task<IActionResult> Index(Guid? lookupId, CancellationToken ct = default)
    {
        var result = await _bookings.GetAsync(lookupId, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new BookingsVm());
        }

        return View(result.Data);
    }

    [HttpPost("provider/bookings/lookup")]
    [ValidateAntiForgeryToken]
    public IActionResult Lookup(Guid? lookupId)
    {
        if (lookupId is null || lookupId == Guid.Empty)
        {
            SetError("Please enter a valid booking id.");
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index), new { lookupId });
    }

    [HttpPost("provider/bookings/join-requests/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveJoin(Guid id, string? responseMessage, CancellationToken ct = default)
    {
        var result = await _bookings.ApproveJoinAsync(id, responseMessage, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Join request approved.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("provider/bookings/join-requests/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectJoin(Guid id, string? responseMessage, CancellationToken ct = default)
    {
        var result = await _bookings.RejectJoinAsync(id, responseMessage, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Join request declined.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("provider/bookings/{id:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBooking(Guid id, CancellationToken ct = default)
    {
        var result = await _bookings.ConfirmBookingAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Booking confirmed.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index), new { lookupId = id });
    }

    [HttpPost("provider/bookings/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBooking(Guid id, string? reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("Please provide a reason for rejecting the booking.");
            return RedirectToAction(nameof(Index), new { lookupId = id });
        }

        var result = await _bookings.RejectBookingAsync(id, reason, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Booking rejected.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Index), new { lookupId = id });
    }

}
