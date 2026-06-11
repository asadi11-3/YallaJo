using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Bookings;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminBookingDashboard.Read)]
public sealed class BookingsController : BaseController
{
    private const int DefaultPageSize = 20;
    private const string WarningKey = "Warning";

    private readonly AdminBookingsFacade _facade;
    private readonly ICurrentUser _currentUser;

    public BookingsController(AdminBookingsFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

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

        // S1/PE1 — same action serves the full page and the listing.js fragment
        // (WantsAjax = X-Requested-With: fetch). No [OutputCache] ever (C2).
        if (!result.IsSuccess || result.Data is null)
        {
            ViewBag.Error = result.Error;
            var fallback = new AdminBookingsIndexVm { Filters = filters };
            return WantsAjax() ? PartialView("_BookingsResults", fallback) : View(fallback);
        }

        return WantsAjax() ? PartialView("_BookingsResults", result.Data) : View(result.Data);
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

    // ── POST /admin/bookings/{id}/force-refund ────────────────────────────────────────
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
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Booking force-refunded.", "Could not force-refund the booking.");
        return RedirectToAction(nameof(Index));
    }

    // ── POST /admin/bookings/{id}/resolve-dispute ─────────────────────────────────────
    // FE-1A-2 (resolve) + FE-1A-3 (optional refund). The refund leg is gated separately:
    // it requires Permission.Refund.Create and is only attempted when the admin opts in.
    [HttpPost("admin/bookings/{id:guid}/resolve-dispute")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BookingDispute.Resolve)]
    public async Task<IActionResult> ResolveDispute(
        Guid id,
        string? resolutionNotes,
        bool issueRefund,
        Guid? paymentId,
        decimal? refundAmount,
        string? refundCurrency,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotes) || resolutionNotes.Trim().Length < 10)
        {
            SetError("Resolution notes are required (at least 10 characters).");
            return RedirectToAction(nameof(Details), new { id });
        }

        RefundRequest? refund = null;
        if (issueRefund)
        {
            // Financial action — enforce the dedicated permission here since the
            // controller-level attribute only covers BookingDispute.Resolve.
            if (!_currentUser.HasPermission(WebPermission.Refund.Create))
            {
                SetError("You don't have permission to issue refunds.");
                return RedirectToAction(nameof(Details), new { id });
            }

            if (paymentId is null || paymentId == Guid.Empty)
            {
                SetError("A payment ID is required to issue a refund.");
                return RedirectToAction(nameof(Details), new { id });
            }

            if (refundAmount is not { } amount || amount <= 0m)
            {
                SetError("A positive refund amount is required.");
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(refundCurrency))
            {
                SetError("A refund currency is required.");
                return RedirectToAction(nameof(Details), new { id });
            }

            refund = new RefundRequest(
                paymentId.Value,
                amount,
                refundCurrency.Trim(),
                $"Dispute resolution: {resolutionNotes.Trim()}");
        }

        var outcome = await _facade.ResolveDisputeAsync(id, resolutionNotes.Trim(), refund, ct);

        if (outcome.RequireSignOut) return RedirectToLogin();

        switch (outcome.Kind)
        {
            case ResolveDisputeOutcome.OutcomeKind.ResolvedNoRefund:
                SetSuccess("Dispute resolved.");
                break;
            case ResolveDisputeOutcome.OutcomeKind.ResolvedAndRefunded:
                SetSuccess("Dispute resolved and refund issued.");
                break;
            case ResolveDisputeOutcome.OutcomeKind.ResolvedButRefundFailed:
                // Partial failure — the dispute is Resolved but the money did NOT move.
                TempData[WarningKey] =
                    $"The dispute was resolved, but the refund failed because {outcome.Message} "
                    + "Please retry the refund manually.";
                break;
            case ResolveDisputeOutcome.OutcomeKind.ResolveFailed:
            default:
                SetError(outcome.Message ?? "Could not resolve the dispute.");
                break;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
