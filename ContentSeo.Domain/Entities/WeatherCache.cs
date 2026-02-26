using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class WeatherCache : BaseEntity
{
    private WeatherCache() { } // EF Core

    public Guid PlaceId { get; private set; }
    public decimal? Temperature { get; private set; }
    public decimal? FeelsLike { get; private set; }
    public int? Humidity { get; private set; }
    public decimal? WindSpeed { get; private set; }
    public int? WindDirection { get; private set; }
    public string? Condition { get; private set; }
    public string? Icon { get; private set; }
    public decimal? UvIndex { get; private set; }
    public string? Forecast { get; private set; }
    public DateTime FetchedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
}
