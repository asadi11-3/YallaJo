using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.TourSchedules;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public enum TourScheduleOutcome
{
    Ok,
    ForceSignOut,
    NotFound,
    Forbidden,
    Conflict,       
    ValidationError, 
    NothingCreated,  
}

public sealed record TourScheduleListResult(
    TourScheduleOutcome Outcome,
    TourSchedulesIndexVm? Data = null,
    string? Error = null);

public sealed record TourScheduleFormResult(
    TourScheduleOutcome Outcome,
    TourScheduleFormVm? Form = null,
    string? Error = null);

public sealed record TourScheduleActionResult(
    TourScheduleOutcome Outcome,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    string? Error = null);

public sealed class ProviderTourSchedulesFacade
{
    private readonly ProviderTourSchedulesApiClient _schedulesApi;
    private readonly ProviderToursApiClient _toursApi;

    public ProviderTourSchedulesFacade(
        ProviderTourSchedulesApiClient schedulesApi, ProviderToursApiClient toursApi)
    {
        _schedulesApi = schedulesApi;
        _toursApi = toursApi;
    }

    public async Task<TourScheduleListResult> GetIndexAsync(Guid tourId, CancellationToken ct = default)
    {
        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return new(TourScheduleOutcome.ForceSignOut);
        if (tour.IsForbidden) return new(TourScheduleOutcome.Forbidden, Error: "You don't have access to this listing.");
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return new(TourScheduleOutcome.NotFound, Error: tour.Error ?? "Listing not found.");

        // activeOnly=false so the owner sees inactive schedules too.
        var schedules = await _schedulesApi.GetSchedulesAsync(tourId, activeOnly: false, ct);
        if (schedules.IsUnauthorized) return new(TourScheduleOutcome.ForceSignOut);
        if (schedules.IsForbidden) return new(TourScheduleOutcome.Forbidden, Error: "You don't have access to this listing's schedules.");
        if (!schedules.IsSuccess || schedules.Data is null)
            return new(TourScheduleOutcome.ValidationError, Error: schedules.Error ?? "Could not load schedules.");

        var vm = TourSchedulesMapper.ToIndexVm(tourId, tour.Data.Name, Humanize(tour.Data.Status), schedules.Data);
        return new(TourScheduleOutcome.Ok, vm);
    }

    public async Task<TourScheduleFormResult> GetCreateAsync(Guid tourId, CancellationToken ct = default)
    {
        var tour = await LoadTourAsync(tourId, ct);
        if (tour.result is not null) return new(tour.result.Value, Error: tour.error);
        return new(TourScheduleOutcome.Ok, TourSchedulesMapper.ToCreateVm(tourId, tour.name!));
    }

    public async Task<TourScheduleActionResult> CreateAsync(Guid tourId, TourScheduleFormVm vm, CancellationToken ct = default)
    {
        var result = await _schedulesApi.CreateAsync(tourId, TourSchedulesMapper.ToCreateRequest(vm), ct);

        if (result.IsSuccess)
        {
            // A 200 with 0 created rows means the recurrence matched nothing — surface it.
            if (result.Data is { Created: 0 })
                return new(TourScheduleOutcome.NothingCreated,
                    Error: "No schedule was added. Please pick a valid day and time and try again.");
            return new(TourScheduleOutcome.Ok);
        }

        return NormalizeAction(result.IsUnauthorized, result.IsForbidden, result.IsNotFound,
            result.IsConflict, result.IsValidationError, result.ValidationErrors, result.Error,
            "Could not create the schedule.");
    }

    public async Task<TourScheduleFormResult> GetEditAsync(Guid tourId, Guid scheduleId, CancellationToken ct = default)
    {
        var tour = await LoadTourAsync(tourId, ct);
        if (tour.result is not null) return new(tour.result.Value, Error: tour.error);

        // No get-by-id endpoint; fetch all (incl. inactive) and pick the row.
        var schedules = await _schedulesApi.GetSchedulesAsync(tourId, activeOnly: false, ct);
        if (schedules.IsUnauthorized) return new(TourScheduleOutcome.ForceSignOut);
        if (!schedules.IsSuccess || schedules.Data is null)
            return new(TourScheduleOutcome.ValidationError, Error: schedules.Error ?? "Could not load the schedule.");

        var schedule = schedules.Data.FirstOrDefault(s => s.Id == scheduleId);
        if (schedule is null) return new(TourScheduleOutcome.NotFound, Error: "Schedule not found.");

        return new(TourScheduleOutcome.Ok, TourSchedulesMapper.ToEditVm(tourId, tour.name!, schedule));
    }

    public async Task<TourScheduleActionResult> UpdateAsync(Guid tourId, Guid scheduleId, TourScheduleFormVm vm, CancellationToken ct = default)
    {
        var result = await _schedulesApi.UpdateAsync(tourId, scheduleId, TourSchedulesMapper.ToUpdateRequest(vm), ct);
        return NormalizeAction(result.IsUnauthorized, result.IsForbidden, result.IsNotFound,
            result.IsConflict, result.IsValidationError, result.ValidationErrors, result.Error,
            "Could not save the schedule.", result.IsSuccess);
    }

    public async Task<TourScheduleActionResult> DeleteAsync(Guid tourId, Guid scheduleId, CancellationToken ct = default)
    {
        var result = await _schedulesApi.DeleteAsync(tourId, scheduleId, ct);
        return NormalizeAction(result.IsUnauthorized, result.IsForbidden, result.IsNotFound,
            result.IsConflict, result.IsValidationError, result.ValidationErrors, result.Error,
            "Could not delete the schedule.", result.IsSuccess);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private async Task<(TourScheduleOutcome? result, string? error, string? name)> LoadTourAsync(Guid tourId, CancellationToken ct)
    {
        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return (TourScheduleOutcome.ForceSignOut, null, null);
        if (tour.IsForbidden) return (TourScheduleOutcome.Forbidden, "You don't have access to this listing.", null);
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return (TourScheduleOutcome.NotFound, tour.Error ?? "Listing not found.", null);
        return (null, null, tour.Data.Name);
    }

    private static TourScheduleActionResult NormalizeAction(
        bool unauthorized, bool forbidden, bool notFound, bool conflict, bool validation,
        IReadOnlyDictionary<string, string[]>? errors, string? error, string fallback, bool success = false)
    {
        if (success) return new(TourScheduleOutcome.Ok);
        if (unauthorized) return new(TourScheduleOutcome.ForceSignOut);
        if (forbidden) return new(TourScheduleOutcome.Forbidden,
            Error: "You don't have permission to manage this listing's schedules.");
        if (notFound) return new(TourScheduleOutcome.NotFound, Error: "Schedule not found.");
        if (conflict) return new(TourScheduleOutcome.Conflict,
            Error: error ?? "This change isn't allowed — the schedule may have future bookings.");
        if (validation) return new(TourScheduleOutcome.ValidationError, ValidationErrors: errors, Error: error);
        return new(TourScheduleOutcome.ValidationError, Error: error ?? fallback);
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
