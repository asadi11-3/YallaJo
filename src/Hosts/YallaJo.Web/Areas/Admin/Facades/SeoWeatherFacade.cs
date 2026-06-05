// <copyright file="SeoWeatherFacade.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Facades;

using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.SeoWeather;
using YallaJo.Web.Infrastructure.Api.Contracts;

public sealed class SeoWeatherFacade
{
    private readonly SeoWeatherApiClient _api;

    public SeoWeatherFacade(SeoWeatherApiClient api) => _api = api;

    public async Task<ApiResult<SeoWeatherVm>> GetAsync(Guid? placeId, CancellationToken ct = default)
    {
        var vm = new SeoWeatherVm
        {
            PlaceId = placeId,
            RefreshForm = new RefreshWeatherFormVm { PlaceId = placeId ?? Guid.Empty },
        };

        if (placeId is null || placeId == Guid.Empty)
        {
            return ApiResult<SeoWeatherVm>.Ok(vm);
        }

        var result = await _api.GetWeatherAsync(placeId.Value, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<SeoWeatherVm>.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            vm.HasQueried = true;
            return ApiResult<SeoWeatherVm>.Ok(vm);
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<SeoWeatherVm>.Fail(result.StatusCode, result.Error ?? "Could not load the weather snapshot.");
        }

        vm.HasQueried = true;
        vm.Weather = SeoWeatherMapper.ToDetail(result.Data);
        return ApiResult<SeoWeatherVm>.Ok(vm);
    }

    public Task<ApiResult> RefreshAsync(RefreshWeatherFormVm form, CancellationToken ct = default)
    {
        var request = new RefreshWeatherApiRequest(form.Latitude, form.Longitude);
        return Normalize(_api.RefreshAsync(form.PlaceId, request, ct), "Could not refresh the weather data.");
    }

    public Task<ApiResult> PurgeAsync(Guid placeId, CancellationToken ct = default)
        => Normalize(_api.PurgeAsync(placeId, ct), "Could not purge the weather cache.");

    public Task<ApiResult> ResetBudgetAsync(DateTime? date, CancellationToken ct = default)
    {
        DateOnly? dateOnly = date is { } d ? DateOnly.FromDateTime(d) : null;
        return Normalize(_api.ResetBudgetAsync(dateOnly, ct), "Could not reset the weather budget.");
    }

    private static Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
        => NormalizeCore(call, fallback);

    private static Task<ApiResult> Normalize<T>(Task<ApiResult<T>> call, string fallback)
        => NormalizeGenericCore(call, fallback);

    private static async Task<ApiResult> NormalizeCore(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        return Map(result.IsSuccess, result.IsUnauthorized, result.IsNotFound, result.IsConflict, result.IsValidationError, result.ValidationErrors, result.StatusCode, result.Error, fallback);
    }

    private static async Task<ApiResult> NormalizeGenericCore<T>(Task<ApiResult<T>> call, string fallback)
    {
        var result = await call;
        return Map(result.IsSuccess, result.IsUnauthorized, result.IsNotFound, result.IsConflict, result.IsValidationError, result.ValidationErrors, result.StatusCode, result.Error, fallback);
    }

    private static ApiResult Map(bool isSuccess, bool isUnauthorized, bool isNotFound, bool isConflict, bool isValidationError, IReadOnlyDictionary<string, string[]>? validationErrors, int statusCode, string? error, string fallback)
    {
        if (isSuccess)
        {
            return ApiResult.Ok();
        }

        if (isUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (isNotFound)
        {
            return ApiResult.Fail(404, "The weather record was not found.");
        }

        if (isConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the current state. Please reload and try again.");
        }

        if (isValidationError && validationErrors is not null)
        {
            return ApiResult.Invalid(validationErrors);
        }

        return ApiResult.Fail(statusCode, error ?? fallback);
    }
}
