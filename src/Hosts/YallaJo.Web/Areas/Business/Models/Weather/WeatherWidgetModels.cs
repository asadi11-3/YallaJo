namespace YallaJo.Web.Areas.Business.Models.Weather;

// GET /api/v1/seo/weather/{placeId} — mirrors ContentSeo WeatherDto.
public sealed class WeatherResponse
{
    public Guid? PlaceId { get; init; }
    public decimal? Temperature { get; init; }
    public decimal? FeelsLike { get; init; }
    public int? Humidity { get; init; }
    public decimal? WindSpeed { get; init; }
    public int? WindDirection { get; init; }
    public string? Condition { get; init; }
    public string? Icon { get; init; }
    public decimal? UvIndex { get; init; }
    public DateTime FetchedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsStale { get; init; }
    public DateTime? StaleSince { get; init; }
    public string? StaleHumanLabel { get; init; }
}

public sealed class WeatherWidgetVm
{
    public bool Available { get; init; }
    public decimal? Temperature { get; init; }
    public decimal? FeelsLike { get; init; }
    public int? Humidity { get; init; }
    public decimal? WindSpeed { get; init; }
    public string? Condition { get; init; }
    public string? Icon { get; init; }
    public bool IsStale { get; init; }
    public string? StaleHumanLabel { get; init; }
}
