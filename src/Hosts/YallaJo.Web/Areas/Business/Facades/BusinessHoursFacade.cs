using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.Hours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class BusinessHoursFacade
{
    private readonly HoursApiClient _api;
    private readonly MyBusinessesApiClient _businesses;
    private readonly IOutputCacheStore _cache;
    private readonly IStringLocalizer<SharedResource> _l;

    public BusinessHoursFacade(HoursApiClient api, MyBusinessesApiClient businesses, IOutputCacheStore cache, IStringLocalizer<SharedResource> localizer)
    {
        _api = api;
        _businesses = businesses;
        _cache = cache;
        _l = localizer;
    }

    public async Task<ApiResult<HoursVm>> GetAsync(Guid businessId, CancellationToken ct = default)
    {
        // API1/D-16: fetch the business detail and the opening hours concurrently.
        var detailTask = _businesses.GetByIdAsync(businessId, ct);
        var hoursTask = _api.GetHoursAsync(businessId, ct);
        await Task.WhenAll(detailTask, hoursTask);

        var detail = await detailTask;
        if (detail.IsUnauthorized)
            return ApiResult<HoursVm>.ForceSignOut();
        if (detail is not { IsSuccess: true, Data: not null })
            return ApiResult<HoursVm>.Fail(detail.StatusCode, detail.Error ?? _l["Business.Error.LoadBusiness"].Value);

        var hours = await hoursTask;
        if (hours.IsUnauthorized)
            return ApiResult<HoursVm>.ForceSignOut();
        if (hours is not { IsSuccess: true, Data: not null })
            return ApiResult<HoursVm>.Fail(hours.StatusCode, hours.Error ?? _l["Business.Error.LoadHours"].Value);

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
        var result = await Normalize(_api.SetHoursAsync(businessId, request, ct), _l["Business.Error.SaveHoursFailed"].Value);
        if (result.IsSuccess) await _cache.EvictByTagAsync($"business:{businessId}", ct);
        return result;
    }

    private async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsForbidden) return ApiResult.Fail(403, _l["Business.Error.NotOwner"].Value);
        if (result.IsNotFound) return ApiResult.Fail(404, _l["Business.Error.BusinessNotFound"].Value);
        if (result.IsConflict) return ApiResult.Fail(409, _l["Business.Error.StateConflict"].Value);
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
