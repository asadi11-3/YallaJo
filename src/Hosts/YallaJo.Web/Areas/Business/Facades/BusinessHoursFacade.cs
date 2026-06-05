using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.Hours;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class BusinessHoursFacade
{
    private readonly HoursApiClient _api;
    private readonly MyBusinessesApiClient _businesses;

    public BusinessHoursFacade(HoursApiClient api, MyBusinessesApiClient businesses)
    {
        _api = api;
        _businesses = businesses;
    }

    public async Task<ApiResult<HoursVm>> GetAsync(Guid businessId, CancellationToken ct = default)
    {
        var detail = await _businesses.GetByIdAsync(businessId, ct);
        if (detail.IsUnauthorized)
            return ApiResult<HoursVm>.ForceSignOut();
        if (detail is not { IsSuccess: true, Data: not null })
            return ApiResult<HoursVm>.Fail(detail.StatusCode, detail.Error ?? "Could not load the business.");

        var hours = await _api.GetHoursAsync(businessId, ct);
        if (hours.IsUnauthorized)
            return ApiResult<HoursVm>.ForceSignOut();
        if (hours is not { IsSuccess: true, Data: not null })
            return ApiResult<HoursVm>.Fail(hours.StatusCode, hours.Error ?? "Could not load the opening hours.");

        var vm = new HoursVm
        {
            BusinessId = businessId,
            BusinessName = detail.Data.Name,
            Status = detail.Data.Status,
            Days = HoursMapper.ToDays(hours.Data),
        };
        return ApiResult<HoursVm>.Ok(vm);
    }

    public async Task<ApiResult> SaveAsync(Guid businessId, IReadOnlyList<DayHoursFormVm> days, CancellationToken ct = default)
    {
        var entries = days
            .Select(d => new HoursEntryApiRequest(
                DayOfWeek: d.DayOfWeek,
                OpenTime: d.IsClosed ? null : NullIfBlank(d.OpenTime),
                CloseTime: d.IsClosed ? null : NullIfBlank(d.CloseTime),
                IsClosed: d.IsClosed))
            .ToList();

        var request = new SetHoursApiRequest(entries);
        return await Normalize(_api.SetHoursAsync(businessId, request, ct), "Could not save the opening hours.");
    }

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsForbidden) return ApiResult.Fail(403, "You do not own this business.");
        if (result.IsNotFound) return ApiResult.Fail(404, "The business was not found.");
        if (result.IsConflict) return ApiResult.Fail(409, "This action is not allowed in the current state. Please reload and try again.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
