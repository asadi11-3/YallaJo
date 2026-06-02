using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class BookingsController : BaseController
{
    private readonly BookingsFacade _bookings;
    private readonly ProfileFacade _profile;

    public BookingsController(BookingsFacade bookings, ProfileFacade profile)
    {
        _bookings = bookings;
        _profile = profile;
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
