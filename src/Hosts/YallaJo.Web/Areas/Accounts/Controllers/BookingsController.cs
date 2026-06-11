using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
public sealed class BookingsController : BaseController
{
    private readonly BookingsFacade _bookings;
    private readonly ProfileFacade _profile;
    private readonly PaymentsFacade _payments;
    private readonly JoinRequestsFacade _joinRequests;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BookingsController(BookingsFacade bookings, ProfileFacade profile, PaymentsFacade payments, JoinRequestsFacade joinRequests, IStringLocalizer<SharedResource> localizer)
    {
        _bookings = bookings;
        _profile = profile;
        _payments = payments;
        _joinRequests = joinRequests;
        _localizer = localizer;
    }

    [HttpGet("accounts/bookings")]
    public async Task<IActionResult> Index(string? tab, string? from, string? to, CancellationToken ct)
    {
        ViewData["AccountNav"] = "Bookings";
        await PopulateSidebarAsync(ct);

        // Phase 3 (Accounts plan): "join-requests" is rendered as an extra tab on My Trips.
        var isJoinRequestsTab = string.Equals(tab, "join-requests", StringComparison.OrdinalIgnoreCase);

        // Phase 4e (Accounts plan): optional date-range filter on the booking tabs (PE1 GET form).
        string? fromDate = null, toDate = null;
        if (!isJoinRequestsTab)
        {
            if (DateOnly.TryParse(from, out var f)) fromDate = f.ToString("yyyy-MM-dd");
            if (DateOnly.TryParse(to, out var t)) toDate = t.ToString("yyyy-MM-dd");
        }

        var result = await _bookings.GetBookingsAsync(isJoinRequestsTab ? null : tab, fromDate, toDate, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        BookingsVm vm;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            vm = new BookingsVm { ActiveTab = tab ?? "Upcoming", FromDate = fromDate, ToDate = toDate };
        }
        else
        {
            vm = result.Data;
        }

        if (isJoinRequestsTab)
        {
            // Re-key the VM to the join-requests tab while keeping the booking tabs for the tab strip.
            vm = new BookingsVm
            {
                ActiveTab = "join-requests",
                Bookings = vm.Bookings,
                Tabs = vm.Tabs,
                JoinRequests = (await _joinRequests.GetMineAsync(ct)) is { IsSuccess: true, Data: { } jr }
                    ? jr
                    : new Models.JoinRequests.MyJoinRequestsVm(),
            };
        }

        return View(vm);
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
            SetSuccess(_localizer["Accounts.Msg.BookingCancelled"]);
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
            SetError(_localizer["Accounts.Msg.DisputeMinLength"]);
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await _bookings.OpenDisputeAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.DisputeSubmitted"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.DisputeOpenFailed"].Value);

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
            SetSuccess(_localizer["Accounts.Msg.PaymentReceived"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.PaymentFailed"].Value);

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
