using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class BookingsController : BaseController
{
    private readonly BookingsFacade _bookings;
    private readonly ProfileFacade _profile;
    private readonly PaymentsFacade _payments;

    public BookingsController(BookingsFacade bookings, ProfileFacade profile, PaymentsFacade payments)
    {
        _bookings = bookings;
        _profile = profile;
        _payments = payments;
    }

    [HttpGet("accounts/bookings")]
    public async Task<IActionResult> Index(string? tab, CancellationToken ct)
    {
        ViewData["AccountNav"] = "Bookings";
        await PopulateSidebarAsync(ct);

        var result = await _bookings.GetBookingsAsync(tab, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new BookingsVm { ActiveTab = tab ?? "Upcoming" });
        }

        return View(result.Data);
    }

    [HttpGet("accounts/bookings/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        ViewData["AccountNav"] = "Bookings";
        await PopulateSidebarAsync(ct);

        var result = await _bookings.GetDetailAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("accounts/bookings/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string? reason, CancellationToken ct)
    {
        var result = await _bookings.CancelAsync(id, reason, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Booking cancelled.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("accounts/bookings/{id:guid}/dispute")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BookingDispute.Create)]
    public async Task<IActionResult> OpenDispute(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
        {
            SetError("Please describe the issue in at least 10 characters.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await _bookings.OpenDisputeAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your dispute has been submitted. Our team will review it shortly.");
        else
            SetError(result.Error ?? "Could not open the dispute.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("accounts/bookings/{id:guid}/pay")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Payment.Create)]
    public async Task<IActionResult> Pay(Guid id, CancellationToken ct)
    {
        // Absolute return URL on this host (the gateway/initiate requires an absolute URI).
        var returnUrl = Url.Action(nameof(Detail), "Bookings", new { area = "Accounts", id }, Request.Scheme)
            ?? $"{Request.Scheme}://{Request.Host}/accounts/bookings/{id}";

        var result = await _payments.PayAsync(id, returnUrl, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Payment received. Your booking is being confirmed — this may take a few moments.");
        else
            SetError(result.Error ?? "Could not process payment.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task PopulateSidebarAsync(CancellationToken ct)
    {
        var profile = await _profile.GetAsync(ct);
        if (profile is { IsSuccess: true, Data: { } p })
        {
            ViewBag.Sidebar = new AccountSidebarVm
            {
                AvatarUrl = p.AvatarUrl,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName)
                    ? $"{p.FirstName} {p.LastName}".Trim()
                    : p.DisplayName,
                Email = p.Email,
            };
        }
        else
        {
            ViewBag.Sidebar = new AccountSidebarVm();
        }
    }
}
