using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Booking;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
public sealed class BookingController : BaseController
{
    private readonly BookingFacade _booking;

    public BookingController(BookingFacade booking) => _booking = booking;

    [HttpGet("tours/{tourId:guid}/book")]
    public async Task<IActionResult> Book(Guid tourId, CancellationToken ct = default)
    {
        var result = await _booking.GetBookingPageAsync(tourId, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
                return NotFound();
            SetError(result.Error);
            return RedirectToAction("Index", "Tours", new { area = "Public" });
        }

        return View(result.Data);
    }

    [HttpPost("tours/{tourId:guid}/book")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(Guid tourId, [Bind(Prefix = "Form")] TourBookingFormVm form, CancellationToken ct = default)
    {
        form.TourId = tourId;

        if (!ModelState.IsValid)
            return await ReloadBookAsync(tourId, form, ct);

        var result = await _booking.CreateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            if (!ApplyValidationErrors(result))
                SetError(result.Error ?? "We could not create your booking. Please try again.");
            return await ReloadBookAsync(tourId, form, ct);
        }

        SetSuccess("Your booking has been created.");
        return RedirectToAction(nameof(Confirmation), new { id = result.Data.Id });
    }

    [HttpGet("bookings/{id:guid}/confirmation")]
    [Authorize]
    public async Task<IActionResult> Confirmation(Guid id, CancellationToken ct = default)
    {
        var result = await _booking.GetConfirmAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
                return NotFound();
            SetError(result.Error);
            return RedirectToAction("Index", "Tours", new { area = "Public" });
        }

        return View(result.Data);
    }

    private async Task<IActionResult> ReloadBookAsync(Guid tourId, TourBookingFormVm form, CancellationToken ct)
    {
        var page = await _booking.GetBookingPageAsync(tourId, ct);
        if (!page.IsSuccess || page.Data is null)
        {
            SetError(page.Error);
            return RedirectToAction("Index", "Tours", new { area = "Public" });
        }

        page.Data.Form = form;
        return View(nameof(Book), page.Data);
    }
}
