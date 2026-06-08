using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.TourWaypoints;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public enum TourWaypointOutcome
{
    Ok,
    ForceSignOut,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError,
}

public sealed record TourWaypointListResult(
    TourWaypointOutcome Outcome,
    TourWaypointsIndexVm? Data = null,
    string? Error = null);

public sealed record TourWaypointFormResult(
    TourWaypointOutcome Outcome,
    TourWaypointFormVm? Form = null,
    string? Error = null);

public sealed record TourWaypointActionResult(
    TourWaypointOutcome Outcome,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    string? Error = null);

public sealed class ProviderTourWaypointsFacade
{
    private readonly ProviderTourWaypointsApiClient _waypointsApi;
    private readonly ProviderToursApiClient _toursApi;
    private readonly IOutputCacheStore _cache;

    public ProviderTourWaypointsFacade(
        ProviderTourWaypointsApiClient waypointsApi, ProviderToursApiClient toursApi, IOutputCacheStore cache)
    {
        _waypointsApi = waypointsApi;
        _toursApi = toursApi;
        _cache = cache;
    }

    public async Task<TourWaypointListResult> GetIndexAsync(Guid tourId, CancellationToken ct = default)
    {
        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return new(TourWaypointOutcome.ForceSignOut);
        if (tour.IsForbidden) return new(TourWaypointOutcome.Forbidden, Error: "You don't have access to this listing.");
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return new(TourWaypointOutcome.NotFound, Error: tour.Error ?? "Listing not found.");

        var waypoints = await _waypointsApi.GetWaypointsAsync(tourId, ct);
        if (waypoints.IsUnauthorized) return new(TourWaypointOutcome.ForceSignOut);
        if (waypoints.IsForbidden) return new(TourWaypointOutcome.Forbidden, Error: "You don't have access to this listing's route.");
        if (!waypoints.IsSuccess || waypoints.Data is null)
            return new(TourWaypointOutcome.ValidationError, Error: waypoints.Error ?? "Could not load waypoints.");

        var vm = TourWaypointsMapper.ToIndexVm(tourId, tour.Data.Name, Humanize(tour.Data.Status), waypoints.Data);
        return new(TourWaypointOutcome.Ok, vm);
    }

    public async Task<TourWaypointFormResult> GetCreateAsync(Guid tourId, CancellationToken ct = default)
    {
        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return new(TourWaypointOutcome.ForceSignOut);
        if (tour.IsForbidden) return new(TourWaypointOutcome.Forbidden, Error: "You don't have access to this listing.");
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return new(TourWaypointOutcome.NotFound, Error: tour.Error ?? "Listing not found.");

        return new(TourWaypointOutcome.Ok, TourWaypointsMapper.ToCreateVm(tourId, tour.Data.Name));
    }

    public async Task<TourWaypointActionResult> CreateAsync(Guid tourId, TourWaypointFormVm vm, CancellationToken ct = default)
    {
        var result = await _waypointsApi.CreateAsync(tourId, TourWaypointsMapper.ToCreateRequest(vm), ct);
        return await EvictOnOkAsync(NormalizeAction(result, "Could not create the waypoint."), tourId, ct);
    }

    public async Task<TourWaypointFormResult> GetEditAsync(Guid tourId, Guid waypointId, CancellationToken ct = default)
    {
        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return new(TourWaypointOutcome.ForceSignOut);
        if (tour.IsForbidden) return new(TourWaypointOutcome.Forbidden, Error: "You don't have access to this listing.");
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return new(TourWaypointOutcome.NotFound, Error: tour.Error ?? "Listing not found.");

        // The waypoints endpoint has no get-by-id; fetch all and pick the waypoint.
        var waypoints = await _waypointsApi.GetWaypointsAsync(tourId, ct);
        if (waypoints.IsUnauthorized) return new(TourWaypointOutcome.ForceSignOut);
        if (!waypoints.IsSuccess || waypoints.Data is null)
            return new(TourWaypointOutcome.ValidationError, Error: waypoints.Error ?? "Could not load the waypoint.");

        var waypoint = waypoints.Data.FirstOrDefault(w => w.Id == waypointId);
        if (waypoint is null) return new(TourWaypointOutcome.NotFound, Error: "Waypoint not found.");

        return new(TourWaypointOutcome.Ok, TourWaypointsMapper.ToEditVm(tourId, tour.Data.Name, waypoint));
    }

    public async Task<TourWaypointActionResult> UpdateAsync(Guid tourId, Guid waypointId, TourWaypointFormVm vm, CancellationToken ct = default)
    {
        var result = await _waypointsApi.UpdateAsync(tourId, waypointId, TourWaypointsMapper.ToUpdateRequest(vm), ct);
        return await EvictOnOkAsync(NormalizeAction(result, "Could not save the waypoint."), tourId, ct);
    }

    public async Task<TourWaypointActionResult> DeleteAsync(Guid tourId, Guid waypointId, CancellationToken ct = default)
    {
        var result = await _waypointsApi.DeleteAsync(tourId, waypointId, ct);
        return await EvictOnOkAsync(NormalizeAction(result, "Could not delete the waypoint."), tourId, ct);
    }

    public async Task<TourWaypointActionResult> ReorderAsync(Guid tourId, IReadOnlyList<Guid> orderedIds, CancellationToken ct = default)
    {
        var result = await _waypointsApi.ReorderAsync(tourId, new ReorderTourWaypointsApiRequest(orderedIds), ct);
        return await EvictOnOkAsync(NormalizeAction(result, "Could not reorder the waypoints."), tourId, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static TourWaypointActionResult NormalizeAction(ApiResult result, string fallback)
    {
        if (result.IsSuccess) return new(TourWaypointOutcome.Ok);
        if (result.IsUnauthorized) return new(TourWaypointOutcome.ForceSignOut);
        if (result.IsForbidden) return new(TourWaypointOutcome.Forbidden,
            Error: "You don't have permission to manage this listing's route.");
        if (result.IsNotFound) return new(TourWaypointOutcome.NotFound, Error: "Waypoint not found.");
        if (result.IsConflict) return new(TourWaypointOutcome.Conflict,
            Error: result.Error ?? "This change isn't allowed in the listing's current state.");
        if (result.IsValidationError) return new(TourWaypointOutcome.ValidationError,
            ValidationErrors: result.ValidationErrors, Error: result.Error);
        return new(TourWaypointOutcome.ValidationError, Error: result.Error ?? fallback);
    }

    private static TourWaypointActionResult NormalizeAction<T>(ApiResult<T> result, string fallback)
    {
        if (result.IsSuccess) return new(TourWaypointOutcome.Ok);
        if (result.IsUnauthorized) return new(TourWaypointOutcome.ForceSignOut);
        if (result.IsForbidden) return new(TourWaypointOutcome.Forbidden,
            Error: "You don't have permission to manage this listing's route.");
        if (result.IsNotFound) return new(TourWaypointOutcome.NotFound, Error: "Listing not found.");
        if (result.IsConflict) return new(TourWaypointOutcome.Conflict,
            Error: result.Error ?? "This change isn't allowed in the listing's current state.");
        if (result.IsValidationError) return new(TourWaypointOutcome.ValidationError,
            ValidationErrors: result.ValidationErrors, Error: result.Error);
        return new(TourWaypointOutcome.ValidationError, Error: result.Error ?? fallback);
    }

    // Evicts the public detail cache tag for the affected tour when the action succeeded.
    // Plan §3 rule #3: facades must evict output-cache tags after successful writes.
    private async Task<TourWaypointActionResult> EvictOnOkAsync(TourWaypointActionResult result, Guid tourId, CancellationToken ct)
    {
        if (result.Outcome == TourWaypointOutcome.Ok)
            await _cache.EvictByTagAsync($"tour:{tourId}", ct);
        return result;
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var sb = new System.Text.StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1])) sb.Append(' ');
            sb.Append(i == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
