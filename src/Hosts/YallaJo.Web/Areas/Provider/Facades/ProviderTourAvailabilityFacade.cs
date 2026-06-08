using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.TourAvailability;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public enum TourAvailabilityOutcome
{
    Ok,
    ForceSignOut,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError,
}

public sealed record TourAvailabilityListResult(
    TourAvailabilityOutcome Outcome,
    TourAvailabilityIndexVm? Data = null,
    string? Error = null);

public sealed record TourAvailabilityFormResult(
    TourAvailabilityOutcome Outcome,
    AvailabilitySlotFormVm? Form = null,
    string? Error = null);

public sealed record TourAvailabilityActionResult(
    TourAvailabilityOutcome Outcome,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    string? Error = null);

public sealed class ProviderTourAvailabilityFacade
{
    private readonly ProviderTourAvailabilityApiClient _availabilityApi;
    private readonly ProviderToursApiClient _toursApi;
    private readonly IOutputCacheStore _cache;

    public ProviderTourAvailabilityFacade(
        ProviderTourAvailabilityApiClient availabilityApi, ProviderToursApiClient toursApi, IOutputCacheStore cache)
    {
        _availabilityApi = availabilityApi;
        _toursApi = toursApi;
        _cache = cache;
    }

    public async Task<TourAvailabilityListResult> GetIndexAsync(Guid tourId, CancellationToken ct = default)
    {
        var tour = await LoadTourAsync(tourId, ct);
        if (tour.result is not null) return new(tour.result.Value, Error: tour.error);

        var slots = await _availabilityApi.GetManageListAsync(tourId, ct);
        if (slots.IsUnauthorized) return new(TourAvailabilityOutcome.ForceSignOut);
        if (slots.IsForbidden)
            return new(TourAvailabilityOutcome.Forbidden, Error: "You don't have access to this listing's availability.");
        if (slots.IsNotFound) return new(TourAvailabilityOutcome.NotFound, Error: "Listing not found.");
        if (!slots.IsSuccess || slots.Data is null)
            return new(TourAvailabilityOutcome.ValidationError, Error: slots.Error ?? "Could not load availability.");

        var vm = TourAvailabilityMapper.ToIndexVm(tourId, tour.name!, Humanize(tour.status!), slots.Data);
        return new(TourAvailabilityOutcome.Ok, vm);
    }

    public async Task<TourAvailabilityFormResult> GetCreateAsync(Guid tourId, CancellationToken ct = default)
    {
        var tour = await LoadTourAsync(tourId, ct);
        if (tour.result is not null) return new(tour.result.Value, Error: tour.error);
        return new(TourAvailabilityOutcome.Ok, TourAvailabilityMapper.ToCreateVm(tourId, tour.name!));
    }

    public async Task<TourAvailabilityActionResult> CreateAsync(
        Guid tourId, AvailabilitySlotFormVm vm, CancellationToken ct = default)
    {
        vm.TourId = tourId;
        var result = await _availabilityApi.CreateAsync(TourAvailabilityMapper.ToCreateRequest(vm), ct);
        if (result.IsSuccess)
        {
            await _cache.EvictByTagAsync($"tour:{tourId}", ct);
            return new(TourAvailabilityOutcome.Ok);
        }

        return NormalizeAction(result.IsUnauthorized, result.IsForbidden, result.IsNotFound,
            result.IsConflict, result.IsValidationError, result.ValidationErrors, result.Error,
            "Could not create the slot.");
    }

    public async Task<TourAvailabilityActionResult> BulkCreateAsync(
        Guid tourId, BulkAvailabilitySlotFormVm vm, CancellationToken ct = default)
    {
        var isCustom = string.Equals(vm.Recurrence, "Custom", StringComparison.OrdinalIgnoreCase);
        var request = new CreateBulkAvailabilitySlotsApiRequest(
            TourId:      tourId,
            StartDate:   vm.StartDate ?? default,
            EndDate:     vm.EndDate ?? default,
            Recurrence:  vm.Recurrence,
            DaysOfWeek:  isCustom ? vm.DaysOfWeek : null,
            StartTime:   vm.StartTime ?? default,
            EndTime:     vm.EndTime ?? default,
            MaxCapacity: vm.MaxCapacity,
            SkipExisting: vm.SkipExisting);

        var result = await _availabilityApi.CreateBulkAsync(request, ct);
        var outcome = NormalizeAction(result.IsUnauthorized, result.IsForbidden, result.IsNotFound,
            result.IsConflict, result.IsValidationError, result.ValidationErrors, result.Error,
            "Could not create the recurring slots.", result.IsSuccess);
        if (outcome.Outcome == TourAvailabilityOutcome.Ok) await _cache.EvictByTagAsync($"tour:{tourId}", ct);
        return outcome;
    }

    public async Task<TourAvailabilityFormResult> GetEditAsync(Guid tourId, Guid slotId, CancellationToken ct = default)
    {
        var tour = await LoadTourAsync(tourId, ct);
        if (tour.result is not null) return new(tour.result.Value, Error: tour.error);

        // No get-by-id endpoint; load the owner manage list and pick the row (carries RowVersion).
        var slots = await _availabilityApi.GetManageListAsync(tourId, ct);
        if (slots.IsUnauthorized) return new(TourAvailabilityOutcome.ForceSignOut);
        if (slots.IsForbidden) return new(TourAvailabilityOutcome.Forbidden, Error: "You don't have access to this listing's availability.");
        if (!slots.IsSuccess || slots.Data is null)
            return new(TourAvailabilityOutcome.ValidationError, Error: slots.Error ?? "Could not load the slot.");

        var slot = slots.Data.FirstOrDefault(s => s.Id == slotId);
        if (slot is null) return new(TourAvailabilityOutcome.NotFound, Error: "Slot not found.");

        return new(TourAvailabilityOutcome.Ok, TourAvailabilityMapper.ToEditVm(tourId, tour.name!, slot));
    }

    public async Task<TourAvailabilityActionResult> UpdateAsync(
        Guid tourId, Guid slotId, AvailabilitySlotFormVm vm, CancellationToken ct = default)
    {
        // Refetch the current RowVersion right before mutating (avoids stale hidden field).
        var fresh = await _availabilityApi.GetManageListAsync(tourId, ct);
        if (fresh.IsUnauthorized) return new(TourAvailabilityOutcome.ForceSignOut);
        if (fresh.IsForbidden) return new(TourAvailabilityOutcome.Forbidden,
            Error: "You don't have permission to manage this listing's availability.");
        if (fresh.IsSuccess && fresh.Data is not null)
        {
            var current = fresh.Data.FirstOrDefault(s => s.Id == slotId);
            if (current is null) return new(TourAvailabilityOutcome.NotFound, Error: "Slot not found.");
            vm.RowVersion = current.RowVersion;
        }

        var result = await _availabilityApi.UpdateAsync(slotId, TourAvailabilityMapper.ToUpdateRequest(vm), ct);
        var outcome = NormalizeAction(result.IsUnauthorized, result.IsForbidden, result.IsNotFound,
            result.IsConflict, result.IsValidationError, result.ValidationErrors, result.Error,
            "Could not save the slot.", result.IsSuccess);
        if (outcome.Outcome == TourAvailabilityOutcome.Ok) await _cache.EvictByTagAsync($"tour:{tourId}", ct);
        return outcome;
    }

    public async Task<TourAvailabilityActionResult> DeleteAsync(
        Guid tourId, Guid slotId, CancellationToken ct = default)
    {
        // Refetch RowVersion before delete.
        var fresh = await _availabilityApi.GetManageListAsync(tourId, ct);
        if (fresh.IsUnauthorized) return new(TourAvailabilityOutcome.ForceSignOut);
        if (fresh.IsForbidden) return new(TourAvailabilityOutcome.Forbidden,
            Error: "You don't have permission to manage this listing's availability.");
        if (!fresh.IsSuccess || fresh.Data is null)
            return new(TourAvailabilityOutcome.ValidationError, Error: fresh.Error ?? "Could not load the slot.");

        var current = fresh.Data.FirstOrDefault(s => s.Id == slotId);
        if (current is null) return new(TourAvailabilityOutcome.NotFound, Error: "Slot not found.");

        var result = await _availabilityApi.DeleteAsync(slotId, current.RowVersion, ct);
        var outcome = NormalizeAction(result.IsUnauthorized, result.IsForbidden, result.IsNotFound,
            result.IsConflict, result.IsValidationError, result.ValidationErrors, result.Error,
            "Could not delete the slot.", result.IsSuccess);
        if (outcome.Outcome == TourAvailabilityOutcome.Ok) await _cache.EvictByTagAsync($"tour:{tourId}", ct);
        return outcome;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private async Task<(TourAvailabilityOutcome? result, string? error, string? name, string? status)> LoadTourAsync(
        Guid tourId, CancellationToken ct)
    {
        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return (TourAvailabilityOutcome.ForceSignOut, null, null, null);
        if (tour.IsForbidden) return (TourAvailabilityOutcome.Forbidden, "You don't have access to this listing.", null, null);
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return (TourAvailabilityOutcome.NotFound, tour.Error ?? "Listing not found.", null, null);
        return (null, null, tour.Data.Name, tour.Data.Status);
    }

    private static TourAvailabilityActionResult NormalizeAction(
        bool unauthorized, bool forbidden, bool notFound, bool conflict, bool validation,
        IReadOnlyDictionary<string, string[]>? errors, string? error, string fallback, bool success = false)
    {
        if (success) return new(TourAvailabilityOutcome.Ok);
        if (unauthorized) return new(TourAvailabilityOutcome.ForceSignOut);
        if (forbidden) return new(TourAvailabilityOutcome.Forbidden,
            Error: "You don't have permission to manage this listing's availability.");
        if (notFound) return new(TourAvailabilityOutcome.NotFound, Error: "Slot not found.");
        if (conflict) return new(TourAvailabilityOutcome.Conflict,
            Error: error ?? "This change isn't allowed — the slot may have bookings, or it was just modified. Reload and try again.");
        if (validation) return new(TourAvailabilityOutcome.ValidationError, ValidationErrors: errors, Error: error);
        return new(TourAvailabilityOutcome.ValidationError, Error: error ?? fallback);
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
