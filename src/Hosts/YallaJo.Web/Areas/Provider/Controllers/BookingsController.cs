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

        if (!result.IsSuccess)
            return await FailAsync(result.Error, lookupId: null, ct);

        return await SucceedAsync("Join request approved.", lookupId: null, ct);
    }

    [HttpPost("provider/bookings/join-requests/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectJoin(Guid id, string? responseMessage, CancellationToken ct = default)
    {
        var result = await _bookings.RejectJoinAsync(id, responseMessage, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
            return await FailAsync(result.Error, lookupId: null, ct);

        return await SucceedAsync("Join request declined.", lookupId: null, ct);
    }

    [HttpPost("provider/bookings/{id:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmBooking(Guid id, CancellationToken ct = default)
    {
        var result = await _bookings.ConfirmBookingAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
            return await FailAsync(result.Error, lookupId: id, ct);

        return await SucceedAsync("Booking confirmed.", lookupId: id, ct);
    }

    [HttpPost("provider/bookings/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBooking(Guid id, string? reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return await FailAsync("Please provide a reason for rejecting the booking.", lookupId: id, ct);

        var result = await _bookings.RejectBookingAsync(id, reason, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess)
            return await FailAsync(result.Error, lookupId: id, ct);

        return await SucceedAsync("Booking rejected.", lookupId: id, ct);
    }

    /// <summary>
    /// AJAX (WantsAjax): re-fetch the page state and return the swappable fragment —
    /// the client toasts its own data-success-message (NF1). No-JS: PRG flash + redirect (PE1).
    /// </summary>
    private async Task<IActionResult> SucceedAsync(string message, Guid? lookupId, CancellationToken ct)
    {
        if (WantsAjax())
        {
            var refreshed = await _bookings.GetAsync(lookupId, ct);
            return PartialView("_BookingsContent", refreshed.Data ?? new BookingsVm());
        }

        SetSuccess(message);
        return RedirectToAction(nameof(Index), lookupId is null ? null : new { lookupId });
    }

    /// <summary>AJAX: 400 + { error } for the client toast. No-JS: PRG flash + redirect.</summary>
    private Task<IActionResult> FailAsync(string? message, Guid? lookupId, CancellationToken ct)
    {
        if (WantsAjax())
            return Task.FromResult<IActionResult>(BadRequest(new { error = message }));

        SetError(message);
        return Task.FromResult<IActionResult>(
            RedirectToAction(nameof(Index), lookupId is null ? null : new { lookupId }));
    }
}
