using YallaJo.Web.Areas.Business.Models.Weather;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Business.ApiClients;

/// <summary>Reads cached, Place-contextual weather (GET /api/v1/seo/weather/{placeId}, §0.2).</summary>
public sealed class WeatherApiClient
{
    private readonly IApiClient _api;

    public WeatherApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<WeatherResponse>> GetByPlaceAsync(Guid placeId, CancellationToken ct = default)
        => _api.GetAsync<WeatherResponse>($"/api/v1/seo/weather/{placeId}", ct);
}
