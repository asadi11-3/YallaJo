using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Bookings;
using YallaJo.Web.Areas.Admin.Models.Payments;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

/// <summary>
/// Composes the admin booking dashboard (read-only). Lists all bookings across
/// providers/travelers and hydrates tour names per distinct TourId (tolerant of
/// failures). No mutations in AB-1.
/// </summary>
public sealed class AdminBookingsFacade
{
    private readonly AdminBookingsApiClient _api;
    private readonly PaymentsApiClient _payments;

    // How many recent payments to scan when resolving a booking's refundable payments.
    private const int PaymentScanPageSize = 100;

    public AdminBookingsFacade(AdminBookingsApiClient api, PaymentsApiClient payments)
    {
        _api = api;
        _payments = payments;
    }

    public async Task<ApiResult<AdminBookingsIndexVm>> GetListAsync(
        AdminBookingFiltersVm filters, string? cursor, int pageSize, CancellationToken ct = default)
    {
        var result = await _api.GetListAsync(filters, cursor, pageSize, ct);
        if (result.IsUnauthorized) return ApiResult<AdminBookingsIndexVm>.ForceSignOut();
        if (result.IsForbidden)
            return ApiResult<AdminBookingsIndexVm>.Fail(403, "You don't have access to the booking dashboard.");
        if (result.IsValidationError)
            return ApiResult<AdminBookingsIndexVm>.Fail(result.StatusCode, result.Error ?? "Invalid filter.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<AdminBookingsIndexVm>.Fail(result.StatusCode, result.Error ?? "Could not load bookings.");

        var tourNames = await HydrateTourNamesAsync(result.Data.Items.Select(i => i.TourId), ct);
        return ApiResult<AdminBookingsIndexVm>.Ok(AdminBookingsMapper.ToIndexVm(result.Data, filters, tourNames));
    }

    public async Task<ApiResult<AdminBookingDetailsVm>> GetDetailsAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetByIdAsync(id, ct);
        if (result.IsUnauthorized) return ApiResult<AdminBookingDetailsVm>.ForceSignOut();
        if (result.IsForbidden)
            return ApiResult<AdminBookingDetailsVm>.Fail(403, "You don't have access to this booking.");
        if (result.IsNotFound) return ApiResult<AdminBookingDetailsVm>.Fail(404, "Booking not found.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<AdminBookingDetailsVm>.Fail(result.StatusCode, result.Error ?? "Could not load the booking.");

        // Resolve the tour name and the booking's refundable payments concurrently (API1).
        var namesTask = HydrateTourNamesAsync([result.Data.TourId], ct);
        var paymentsTask = LoadBookingPaymentsAsync(id, ct);
        await Task.WhenAll(namesTask, paymentsTask);

        var names = await namesTask;
        var tourName = names.TryGetValue(result.Data.TourId, out var n) && !string.IsNullOrWhiteSpace(n)
            ? n
            : $"Tour {result.Data.TourId.ToString("N")[..8]}";

        var payments = await paymentsTask;
        return ApiResult<AdminBookingDetailsVm>.Ok(
            AdminBookingsMapper.ToDetailsVm(result.Data, tourName, payments));
    }

    /// <summary>
    /// Best-effort load of this booking's refundable (Completed) payments so the
    /// resolve-dispute refund leg can offer a real payment <see langword="select"/>
    /// (F10) instead of a raw GUID input. Tolerant: any failure yields an empty list,
    /// which collapses the refund leg to resolve-only.
    /// </summary>
    private async Task<IReadOnlyList<PaymentResponse>> LoadBookingPaymentsAsync(
        Guid bookingId, CancellationToken ct)
    {
        try
        {
            var result = await _payments.GetAdminPaymentsAsync(
                status: "Completed", type: null, cursor: null,
                pageSize: PaymentScanPageSize, ct: ct, bookingId: bookingId);

            if (!result.IsSuccess || result.Data is null)
                return [];

            // Client-side filter as a safety net in case the API ignores bookingId.
            return result.Data.Items
                .Where(p => p.BookingId == bookingId)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    // POST /api/v1/admin/bookings/{id}/force-refund — force-majeure cancel + full refund (E4).
    public async Task<ApiResult> ForceRefundAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var result = await _api.ForceRefundAsync(id, reason, ct);
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsForbidden) return ApiResult.Fail(403, "You don't have access to this action.");
        if (result.IsNotFound) return ApiResult.Fail(404, "Booking not found.");
        if (result.IsConflict)
            return ApiResult.Fail(409, "This booking can no longer be force-refunded in its current state. Please reload and try again.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not force-refund the booking.");
    }

    /// <summary>
    /// FE-1A-2 / FE-1A-3: resolve a disputed booking and, optionally, issue a payment
    /// refund as a SECOND step. The two backend calls are independent (no distributed
    /// transaction), so partial failure is possible and surfaced explicitly:
    ///   • resolve fails → refund is NOT attempted; returns the resolve error.
    ///   • resolve succeeds + refund requested + refund fails → returns a 207-style
    ///     partial result so the admin knows the dispute is Resolved but the refund
    ///     must be retried manually.
    /// </summary>
    public async Task<ResolveDisputeOutcome> ResolveDisputeAsync(
        Guid bookingId,
        string resolutionNotes,
        RefundRequest? refund,
        CancellationToken ct = default)
    {
        var resolve = await _api.ResolveDisputeAsync(
            bookingId, new ResolveBookingDisputeRequest(resolutionNotes), ct);

        if (resolve.IsUnauthorized)
            return ResolveDisputeOutcome.SignOut();
        if (!resolve.IsSuccess)
            return ResolveDisputeOutcome.ResolveFailed(MapResolveError(resolve));

        // Resolve succeeded. If no refund requested, we're done.
        if (refund is null)
            return ResolveDisputeOutcome.ResolvedNoRefund();

        var refundResult = await _api.RefundPaymentAsync(
            refund.PaymentId,
            new RefundPaymentRequest(refund.Amount, refund.Currency, refund.Reason),
            ct);

        if (refundResult.IsSuccess)
            return ResolveDisputeOutcome.ResolvedAndRefunded();

        // CRITICAL: the dispute is already Resolved but the refund failed. Do NOT
        // pretend the whole thing failed — tell the admin precisely what happened.
        return ResolveDisputeOutcome.ResolvedButRefundFailed(MapRefundError(refundResult));
    }

    private static string MapResolveError(ApiResult r)
    {
        if (r.IsForbidden) return "You don't have permission to resolve disputes.";
        if (r.IsNotFound) return "Booking not found.";
        if (r.IsConflict) return "This booking was just updated. Please reload and try again.";
        if (r.StatusCode is 422 or 400)
            return r.Error ?? "This booking is not in a disputable state.";
        return r.Error ?? "Could not resolve the dispute.";
    }

    private static string MapRefundError(ApiResult r)
    {
        if (r.IsForbidden) return "the refund was rejected (insufficient permission).";
        if (r.IsNotFound) return "the payment could not be found.";
        if (r.StatusCode == 502) return "the payment gateway is unavailable.";
        if (r.IsConflict) return "the payment is in a state that cannot be refunded.";
        if (r.StatusCode is 422 or 400) return r.Error ?? "the refund amount was invalid.";
        return r.Error ?? "the refund could not be issued.";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private async Task<IReadOnlyDictionary<Guid, string>> HydrateTourNamesAsync(
        IEnumerable<Guid> tourIds, CancellationToken ct)
    {
        var distinct = tourIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var map = new Dictionary<Guid, string>();
        if (distinct.Count == 0) return map;

        var tasks = distinct.Select(async id =>
        {
            try
            {
                var r = await _api.GetTourAsync(id, ct);
                return (id, name: r is { IsSuccess: true, Data: { } t } && !string.IsNullOrWhiteSpace(t.Name)
                    ? t.Name : string.Empty);
            }
            catch
            {
                return (id, name: string.Empty);
            }
        });

        foreach (var (id, name) in await Task.WhenAll(tasks))
            map[id] = name;

        return map;
    }
}
