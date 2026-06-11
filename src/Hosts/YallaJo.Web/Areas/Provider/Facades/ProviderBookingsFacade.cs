using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Bookings;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public enum ProviderBookingOutcome
{
    Ok,
    ForceSignOut,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError,
}

public sealed record ProviderBookingListResult(
    ProviderBookingOutcome Outcome,
    ProviderBookingsIndexVm? Data = null,
    string? Error = null);

public sealed record ProviderBookingDetailsResult(
    ProviderBookingOutcome Outcome,
    ProviderBookingDetailsVm? Data = null,
    string? Error = null);

public sealed record ProviderBookingActionResult(
    ProviderBookingOutcome Outcome,
    string? Error = null);

public sealed class ProviderBookingsFacade
{
    private readonly ProviderBookingsApiClient _api;
    private readonly ProviderToursApiClient _tours;

    public ProviderBookingsFacade(ProviderBookingsApiClient api, ProviderToursApiClient tours)
    {
        _api = api;
        _tours = tours;
    }

    public async Task<ProviderBookingListResult> GetListAsync(
        string? status, string? cursor = null,
        string? fromDate = null, string? toDate = null, Guid? tourId = null,
        CancellationToken ct = default)
    {
        // API1: bookings page + tour filter options in parallel.
        var listTask = _api.GetListAsync(status, cursor, fromDate, toDate, tourId, ct);
        var toursTask = _tours.GetMyToursAsync(1, 100, status: null, sort: null, ct);
        await Task.WhenAll(listTask, toursTask);

        var result = listTask.Result;
        if (result.IsUnauthorized) return new(ProviderBookingOutcome.ForceSignOut);
        if (result.IsForbidden) return new(ProviderBookingOutcome.Forbidden, Error: "You don't have access to provider bookings.");
        if (result.IsValidationError) return new(ProviderBookingOutcome.ValidationError, Error: result.Error ?? "Invalid filter.");
        if (!result.IsSuccess || result.Data is null)
            return new(ProviderBookingOutcome.ValidationError, Error: result.Error ?? "Could not load bookings.");

        // Filter dropdown options — soft-degrade to empty list on failure (ERR3).
        IReadOnlyList<BookingTourFilterOptionVm> tourOptions = [];
        var toursResult = toursTask.Result;
        if (toursResult.IsSuccess && toursResult.Data is not null)
        {
            tourOptions = toursResult.Data.Items
                .Select(t => new BookingTourFilterOptionVm { Id = t.Id, Name = t.Name })
                .ToList();
        }

        var tourNames = await HydrateTourNamesAsync(result.Data.Items.Select(i => i.TourId), ct);
        return new(ProviderBookingOutcome.Ok,
            ProviderBookingsMapper.ToIndexVm(result.Data, status, tourNames, fromDate, toDate, tourId, tourOptions));
    }

    public async Task<ProviderBookingDetailsResult> GetDetailsAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetByIdAsync(id, ct);
        if (result.IsUnauthorized) return new(ProviderBookingOutcome.ForceSignOut);
        if (result.IsForbidden) return new(ProviderBookingOutcome.Forbidden, Error: "You don't have access to this booking.");
        if (result.IsNotFound) return new(ProviderBookingOutcome.NotFound, Error: "Booking not found.");
        if (!result.IsSuccess || result.Data is null)
            return new(ProviderBookingOutcome.ValidationError, Error: result.Error ?? "Could not load the booking.");

        var names = await HydrateTourNamesAsync([result.Data.TourId], ct);
        var tourName = names.TryGetValue(result.Data.TourId, out var n) ? n : "Tour booking";

        // Detail DTO carries no slot times; the list view shows them. Keep details concise.
        return new(ProviderBookingOutcome.Ok,
            ProviderBookingsMapper.ToDetailsVm(result.Data, tourName, slotLabel: "—"));
    }

    public async Task<ProviderBookingActionResult> ConfirmAsync(Guid id, CancellationToken ct = default)
        => Normalize(await _api.ConfirmAsync(id, ct), "Could not confirm the booking.");

    public async Task<ProviderBookingActionResult> CancelAsync(Guid id, string reason, CancellationToken ct = default)
        => Normalize(await _api.CancelAsync(id, reason, ct), "Could not cancel the booking.");

    public async Task<ProviderBookingActionResult> RejectAsync(Guid id, string reason, CancellationToken ct = default)
        => Normalize(await _api.RejectAsync(id, reason, ct), "Could not reject the booking.");

    public async Task<ProviderBookingActionResult> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.CompleteAsync(id, ct);
        if (result.IsSuccess) return new(ProviderBookingOutcome.Ok);
        if (result.IsUnauthorized) return new(ProviderBookingOutcome.ForceSignOut);
        if (result.IsForbidden) return new(ProviderBookingOutcome.Forbidden,
            Error: "Only the provider of this tour (or an admin) can complete this booking.");
        if (result.IsNotFound) return new(ProviderBookingOutcome.NotFound, Error: "Booking not found.");
        if (result.IsConflict) return new(ProviderBookingOutcome.Conflict,
            Error: "The booking was changed by another request. Reload and try again.");

        // The backend returns 400 for both "not yet started" and "invalid state".
        // The non-generic ApiResult pipeline does not preserve the backend Detail on a
        // 400, so we surface one clear, actionable message that covers the dominant case
        // (a Confirmed booking whose tour has not started yet) without misleading wording.
        if (result.IsValidationError)
        {
            return new(ProviderBookingOutcome.ValidationError,
                Error: result.Error is { Length: > 0 } e
                    ? e
                    : "This booking can't be completed yet — a tour can only be completed after it has started.");
        }

        return new(ProviderBookingOutcome.ValidationError,
            Error: result.Error ?? "Could not complete the booking.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private async Task<IReadOnlyDictionary<Guid, string>> HydrateTourNamesAsync(
        IEnumerable<Guid> tourIds, CancellationToken ct)
    {
        var distinct = tourIds.Distinct().ToList();
        var map = new Dictionary<Guid, string>();
        if (distinct.Count == 0) return map;

        var tasks = distinct.Select(async id =>
        {
            try
            {
                var r = await _api.GetTourAsync(id, ct);
                return (id, name: r is { IsSuccess: true, Data: { } t } && !string.IsNullOrWhiteSpace(t.Name)
                    ? t.Name : "Tour booking");
            }
            catch
            {
                return (id, name: "Tour booking");
            }
        });

        foreach (var (id, name) in await Task.WhenAll(tasks))
            map[id] = name;

        return map;
    }

    private static ProviderBookingActionResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess) return new(ProviderBookingOutcome.Ok);
        if (result.IsUnauthorized) return new(ProviderBookingOutcome.ForceSignOut);
        if (result.IsForbidden) return new(ProviderBookingOutcome.Forbidden,
            Error: "Only the provider of this tour can perform this action.");
        if (result.IsNotFound) return new(ProviderBookingOutcome.NotFound, Error: "Booking not found.");
        if (result.IsConflict) return new(ProviderBookingOutcome.Conflict,
            Error: result.Error ?? "This action isn't allowed in the booking's current state. Reload and try again.");
        if (result.IsValidationError) return new(ProviderBookingOutcome.ValidationError,
            Error: result.Error ?? "Some details are invalid. Please review and try again.");
        return new(ProviderBookingOutcome.ValidationError, Error: result.Error ?? fallback);
    }
}
