using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public sealed class TourFacade
{
    private readonly TourApiClient _api;

    public TourFacade(TourApiClient api) => _api = api;

    private static readonly string[] DayNames =
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    public async Task<ApiResult<CreateTourResultResponse>> CreateAsync(CreateTourVm vm, CancellationToken ct = default)
    {
        var request = new CreateTourRequest(
            Name: vm.Name.Trim(),
            Slug: vm.Slug.Trim(),
            Difficulty: vm.Difficulty,
            DurationMinutes: vm.DurationMinutes,
            MaxGroupSize: vm.MaxGroupSize,
            BasePrice: vm.BasePrice,
            Currency: vm.Currency.Trim().ToUpperInvariant(),
            Latitude: vm.Latitude,
            Longitude: vm.Longitude,
            Description: string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
            ShortDescription: string.IsNullOrWhiteSpace(vm.ShortDescription) ? null : vm.ShortDescription.Trim(),
            MinAge: vm.MinAge,
            IsChildFriendly: vm.IsChildFriendly,
            IsAccessible: vm.IsAccessible,
            IsInstantBooking: vm.IsInstantBooking,
            CancellationPolicyHours: vm.CancellationPolicyHours);

        var result = await _api.CreateAsync(request, ct);
        if (result.IsUnauthorized) return ApiResult<CreateTourResultResponse>.ForceSignOut();
        if (result is { IsSuccess: true, Data: not null }) return ApiResult<CreateTourResultResponse>.Ok(result.Data, result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult<CreateTourResultResponse>.ValidationFail(result.StatusCode, result.ValidationErrors);
        return ApiResult<CreateTourResultResponse>.Fail(result.StatusCode, result.Error ?? "Could not create the listing.");
    }

    public async Task<ApiResult<ManageTourVm>> GetManageAsync(Guid id, CancellationToken ct = default)
    {
        var tourResult = await _api.GetTourAsync(id, ct);
        if (tourResult.IsUnauthorized) return ApiResult<ManageTourVm>.ForceSignOut();
        if (tourResult is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<ManageTourVm>.Fail(
                tourResult.StatusCode,
                tourResult.IsNotFound ? "Listing not found." : tourResult.Error ?? "Could not load the listing.");
        }

        var tour = tourResult.Data;

        var schedulesTask = SafeListAsync(() => _api.GetSchedulesAsync(id, ct));
        var pricingTask = SafeListAsync(() => _api.GetPricingAsync(id, ct));
        var waypointsTask = SafeListAsync(() => _api.GetWaypointsAsync(id, ct));
        var childrenTask = SafeChildrenAsync(() => _api.GetChildrenInfoAsync(id, ct));

        await Task.WhenAll(schedulesTask, pricingTask, waypointsTask, childrenTask);

        var schedules = schedulesTask.Result
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new ScheduleRowVm
            {
                Id = s.Id,
                DayName = s.DayOfWeek < DayNames.Length ? DayNames[s.DayOfWeek] : $"Day {s.DayOfWeek}",
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsActive = s.IsActive,
            }).ToList();

        var pricing = pricingTask.Result
            .Select(p => new PricingRowVm
            {
                Id = p.Id,
                Name = p.Name,
                ParticipantType = p.ParticipantType,
                Price = p.Price,
                Currency = p.Currency,
                MinParticipants = p.MinParticipants,
                MaxParticipants = p.MaxParticipants,
                IsActive = p.IsActive,
            }).ToList();

        var waypoints = waypointsTask.Result
            .OrderBy(w => w.SortOrder)
            .Select(w => new WaypointRowVm
            {
                Id = w.Id,
                Name = w.Name,
                WaypointType = w.WaypointType,
                SortOrder = w.SortOrder,
                DurationMinutes = w.DurationMinutes,
            }).ToList();

        var children = childrenTask.Result;
        var childrenVm = new ChildrenInfoVm
        {
            AllowsChildren = children?.AllowsChildren ?? false,
            MinChildAge = children?.MinChildAge,
            MaxChildAge = children?.MaxChildAge,
            ChildFacilities = children is { ChildFacilities.Count: > 0 } ? string.Join(", ", children.ChildFacilities) : null,
        };

        var vm = new ManageTourVm
        {
            TourId = tour.Id,
            Name = tour.Name,
            Slug = tour.Slug,
            Status = tour.Status,
            BasePrice = tour.BasePrice,
            Currency = tour.Currency,
            ReviewCount = tour.ReviewCount,
            BookingCount = tour.BookingCount,
            Schedules = schedules,
            PricingTiers = pricing,
            Waypoints = waypoints,
            ChildrenInfo = childrenVm,
        };

        return ApiResult<ManageTourVm>.Ok(vm);
    }

    public Task<ApiResult> AddScheduleAsync(Guid id, AddScheduleVm vm, CancellationToken ct = default)
    {
        var request = new CreateTourScheduleRequest(
            Pattern: "Weekly",
            DaysOfWeek: [vm.DayOfWeek],
            CustomDates: null,
            StartTime: vm.StartTime,
            EndTime: vm.EndTime,
            ValidFrom: null,
            ValidTo: null,
            IsActive: true);
        return NormalizeAsync(() => _api.AddScheduleAsync(id, request, ct), "Could not add the schedule.");
    }

    public Task<ApiResult> DeleteScheduleAsync(Guid id, Guid scheduleId, CancellationToken ct = default)
        => NormalizeAsync(() => _api.DeleteScheduleAsync(id, scheduleId, ct), "Could not remove the schedule.");

    public Task<ApiResult> AddPricingAsync(Guid id, AddPricingVm vm, CancellationToken ct = default)
    {
        var request = new CreateTourPricingTierRequest(
            Name: vm.Name.Trim(),
            Description: null,
            Price: vm.Price,
            Currency: vm.Currency.Trim().ToUpperInvariant(),
            ParticipantType: vm.ParticipantType,
            MinParticipants: vm.MinParticipants,
            MaxParticipants: vm.MaxParticipants);
        return NormalizeAsync(() => _api.AddPricingAsync(id, request, ct), "Could not add the pricing tier.");
    }

    public Task<ApiResult> DeletePricingAsync(Guid id, Guid tierId, CancellationToken ct = default)
        => NormalizeAsync(() => _api.DeletePricingAsync(id, tierId, ct), "Could not remove the pricing tier.");

    public Task<ApiResult> AddWaypointAsync(Guid id, AddWaypointVm vm, CancellationToken ct = default)
    {
        var request = new AddTourWaypointRequest(
            Name: vm.Name.Trim(),
            Description: null,
            Latitude: vm.Latitude,
            Longitude: vm.Longitude,
            WaypointType: vm.WaypointType,
            DurationMinutes: vm.DurationMinutes);
        return NormalizeAsync(() => _api.AddWaypointAsync(id, request, ct), "Could not add the waypoint.");
    }

    public Task<ApiResult> DeleteWaypointAsync(Guid id, Guid waypointId, CancellationToken ct = default)
        => NormalizeAsync(() => _api.DeleteWaypointAsync(id, waypointId, ct), "Could not remove the waypoint.");

    public Task<ApiResult> UpdateChildrenInfoAsync(Guid id, ChildrenInfoVm vm, CancellationToken ct = default)
    {
        var request = new UpdateChildrenInfoRequest(
            AllowsChildren: vm.AllowsChildren,
            MinChildAge: vm.MinChildAge,
            MaxChildAge: vm.MaxChildAge,
            ChildFacilities: string.IsNullOrWhiteSpace(vm.ChildFacilities) ? null : vm.ChildFacilities.Trim());
        return NormalizeAsync(() => _api.UpdateChildrenInfoAsync(id, request, ct), "Could not update children information.");
    }

    private static async Task<ApiResult> NormalizeAsync(Func<Task<ApiResult>> call, string fallback)
    {
        ApiResult result;
        try
        {
            result = await call();
        }
        catch
        {
            return ApiResult.Fail(500, fallback);
        }

        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsSuccess) return ApiResult.Ok(result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static async Task<List<T>> SafeListAsync<T>(Func<Task<ApiResult<List<T>>>> call)
    {
        try
        {
            var result = await call();
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task<ChildrenInfoResponse?> SafeChildrenAsync(Func<Task<ApiResult<ChildrenInfoResponse>>> call)
    {
        try
        {
            var result = await call();
            return result is { IsSuccess: true, Data: not null } ? result.Data : null;
        }
        catch
        {
            return null;
        }
    }
}
