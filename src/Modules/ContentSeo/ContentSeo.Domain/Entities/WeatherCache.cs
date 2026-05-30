// <copyright file="WeatherCache.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class WeatherCache : AuditableEntity, IAggregateRoot
{
    private WeatherCache()
    {
    } // EF Core

    /// <summary>
    /// Optional: the Place this cache row was fetched for.
    /// Nullable because PDF §11 keys by coordinates, not PlaceId.
    /// </summary>
    public Guid? PlaceId { get; private set; }

    /// <summary>
    /// PDF §11: cache key = (lat rounded to 2 dp, lng rounded to 2 dp, date).
    /// Nearby tours share the same cache row.
    /// </summary>
    public decimal RoundedLatitude { get; private set; }

    /// <summary>PDF §11: longitude component of the composite cache key.</summary>
    public decimal RoundedLongitude { get; private set; }

    /// <summary>PDF §11: date component of the composite cache key (UTC date).</summary>
    public DateOnly ForecastDate { get; private set; }

    public decimal? Temperature { get; private set; }

    public decimal? FeelsLike { get; private set; }

    public int? Humidity { get; private set; }

    public decimal? WindSpeed { get; private set; }

    public int? WindDirection { get; private set; }

    public string? Condition { get; private set; }

    public string? Icon { get; private set; }

    public decimal? UvIndex { get; private set; }

    /// <summary>
    /// JSON-serialised array of <c>DailyForecast</c> objects.
    /// PDF §11: 7-day forecast horizon from today.
    /// </summary>
    public string? ForecastJson { get; private set; }

    public DateTime FetchedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Rounds a coordinate to 2 decimal places (PDF §11 cache-key rule).
    /// </summary>
    public static decimal RoundCoordinate(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Creates a new WeatherCache row keyed by rounded coordinates + date.
    /// PDF §11: cache key = (lat-rounded-2dp, lng-rounded-2dp, date).
    /// </summary>
    public static WeatherCache Create(
        decimal latitude,
        decimal longitude,
        DateOnly forecastDate,
        Guid? placeId,
        decimal? temperature,
        decimal? feelsLike,
        int? humidity,
        decimal? windSpeed,
        int? windDirection,
        string? condition,
        string? icon,
        decimal? uvIndex,
        string? forecastJson,
        DateTime fetchedAt,
        DateTime expiresAt)
    {
        if (expiresAt <= fetchedAt)
        {
            throw new ArgumentException("ExpiresAt must be greater than FetchedAt.", nameof(expiresAt));
        }

        var cache = new WeatherCache
        {
            Id = Guid.CreateVersion7(),
            PlaceId = placeId,
            RoundedLatitude = RoundCoordinate(latitude),
            RoundedLongitude = RoundCoordinate(longitude),
            ForecastDate = forecastDate,
            Temperature = temperature,
            FeelsLike = feelsLike,
            Humidity = humidity,
            WindSpeed = windSpeed,
            WindDirection = windDirection,
            Condition = Trim(condition),
            Icon = Trim(icon),
            UvIndex = uvIndex,
            ForecastJson = forecastJson,
            FetchedAt = fetchedAt,
            ExpiresAt = expiresAt,
        };
        return cache;
    }

    /// <summary>
    /// Refreshes an existing cache row in place with new snapshot data.
    /// </summary>
    public void Refresh(
        decimal? temperature,
        decimal? feelsLike,
        int? humidity,
        decimal? windSpeed,
        int? windDirection,
        string? condition,
        string? icon,
        decimal? uvIndex,
        string? forecastJson,
        DateTime fetchedAt,
        DateTime expiresAt)
    {
        EnsureNotDeleted();
        if (expiresAt <= fetchedAt)
        {
            throw new ArgumentException("ExpiresAt must be greater than FetchedAt.", nameof(expiresAt));
        }

        Temperature = temperature;
        FeelsLike = feelsLike;
        Humidity = humidity;
        WindSpeed = windSpeed;
        WindDirection = windDirection;
        Condition = Trim(condition);
        Icon = Trim(icon);
        UvIndex = uvIndex;
        ForecastJson = forecastJson;
        FetchedAt = fetchedAt;
        ExpiresAt = expiresAt;
        MarkUpdated();
    }

    public bool IsStale(DateTime nowUtc) => nowUtc >= ExpiresAt;

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("WeatherCache.Deleted: cannot mutate a deleted cache row.");
        }
    }
}
