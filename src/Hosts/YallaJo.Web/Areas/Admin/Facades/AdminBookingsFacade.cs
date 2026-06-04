using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Bookings;
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

    public AdminBookingsFacade(AdminBookingsApiClient api) => _api = api;

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

        var names = await HydrateTourNamesAsync([result.Data.TourId], ct);
        var tourName = names.TryGetValue(result.Data.TourId, out var n) && !string.IsNullOrWhiteSpace(n)
            ? n
            : $"Tour {result.Data.TourId.ToString("N")[..8]}";

        return ApiResult<AdminBookingDetailsVm>.Ok(AdminBookingsMapper.ToDetailsVm(result.Data, tourName));
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
