using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.Weather;

namespace YallaJo.Web.Areas.Business.Facades;

/// <summary>
/// Place-contextual weather for the business widget. Non-blocking by design (ERR3):
/// any failure degrades to an unavailable widget rather than breaking the page.
/// </summary>
public sealed class BusinessWeatherFacade
{
    private readonly WeatherApiClient _api;

    public BusinessWeatherFacade(WeatherApiClient api) => _api = api;

    public async Task<WeatherWidgetVm> GetForPlaceAsync(Guid placeId, CancellationToken ct = default)
    {
        if (placeId == Guid.Empty)
            return new WeatherWidgetVm { Available = false };

        try
        {
            var result = await _api.GetByPlaceAsync(placeId, ct);
            if (!result.IsSuccess || result.Data is null)
                return new WeatherWidgetVm { Available = false };

            var d = result.Data;
            return new WeatherWidgetVm
            {
                Available       = d.Temperature.HasValue || !string.IsNullOrWhiteSpace(d.Condition),
                Temperature     = d.Temperature,
                FeelsLike       = d.FeelsLike,
                Humidity        = d.Humidity,
                WindSpeed       = d.WindSpeed,
                Condition       = d.Condition,
                Icon            = d.Icon,
                IsStale         = d.IsStale,
                StaleHumanLabel = d.StaleHumanLabel,
            };
        }
        catch
        {
            return new WeatherWidgetVm { Available = false };
        }
    }
}
