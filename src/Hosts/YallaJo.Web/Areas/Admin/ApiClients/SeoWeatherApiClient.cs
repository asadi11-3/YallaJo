// <copyright file="SeoWeatherApiClient.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.ApiClients;

using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.SeoWeather;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

public sealed class SeoWeatherApiClient
{
    private const string Base = "/api/v1/seo/weather";

    private readonly IApiClient _api;

    public SeoWeatherApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<WeatherItemResponse>> GetWeatherAsync(Guid placeId, CancellationToken ct = default)
        => _api.GetAsync<WeatherItemResponse>($"{Base}/{placeId:D}", ct);

    public Task<ApiResult<RefreshWeatherResponse>> RefreshAsync(Guid placeId, RefreshWeatherApiRequest request, CancellationToken ct = default)
        => _api.PostAsync<RefreshWeatherResponse>($"{Base}/refresh/{placeId:D}", request, ct);

    public Task<ApiResult> PurgeAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/cache/{id:D}", ct);

    public Task<ApiResult> ResetBudgetAsync(DateOnly? date, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>();
        if (date is { } d)
        {
            query["date"] = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var url = QueryHelpers.AddQueryString($"{Base}/budget/reset", query);
        return _api.PutAsync(url, null, ct);
    }
}
