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

    /// <summary>
    /// Creates a new WeatherCache row for the given place.
    /// </summary>
    public static WeatherCache Create(
        Guid placeId,
        decimal? temperature,
        decimal? feelsLike,
        int? humidity,
        decimal? windSpeed,
        int? windDirection,
        string? condition,
        string? icon,
        decimal? uvIndex,
        string? forecast,
        DateTime fetchedAt,
        DateTime expiresAt)
    {
        if (placeId == Guid.Empty)
        {
            throw new ArgumentException("PlaceId is required.", nameof(placeId));
        }

        if (expiresAt <= fetchedAt)
        {
            throw new ArgumentException("ExpiresAt must be greater than FetchedAt.", nameof(expiresAt));
        }

        var cache = new WeatherCache
        {
            Id = Guid.CreateVersion7(),
            PlaceId = placeId,
            Temperature = temperature,
            FeelsLike = feelsLike,
            Humidity = humidity,
            WindSpeed = windSpeed,
            WindDirection = windDirection,
            Condition = Trim(condition),
            Icon = Trim(icon),
            UvIndex = uvIndex,
            Forecast = forecast,
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
        string? forecast,
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
        Forecast = forecast;
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
