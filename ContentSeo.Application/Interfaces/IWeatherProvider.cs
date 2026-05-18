// <copyright file="IWeatherProvider.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Interfaces;

/// <summary>
/// Abstraction over an upstream weather data provider (e.g. OpenWeatherMap).
/// The real implementation lands in Wave 6; v1 uses a NoOp.
/// </summary>
public interface IWeatherProvider
{
    /// <summary>Whether the provider is configured and available.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Fetches current conditions + a 7-day forecast for the given coordinates.
    /// PDF §11: forecast horizon is exactly 7 days from today.
    /// </summary>
    Task<WeatherSnapshot> FetchAsync(decimal latitude, decimal longitude, CancellationToken ct);
}

/// <summary>
/// Current-conditions snapshot plus a 7-day daily forecast.
/// PDF §11: display temp °C, weather icon, condition text, humidity %.
/// </summary>
public sealed record WeatherSnapshot(
    decimal Temperature,
    decimal FeelsLike,
    int Humidity,
    decimal WindSpeed,
    int WindDirection,
    string Condition,
    string IconCode,
    decimal? UvIndex,
    IReadOnlyList<DailyForecast> DailyForecasts);

/// <summary>
/// One day in the 7-day forecast.
/// PDF §11: temp °C, weather icon, condition text, humidity %.
/// </summary>
public sealed record DailyForecast(
    DateOnly Date,
    decimal HighTemp,
    decimal LowTemp,
    string Condition,
    string IconCode,
    int Humidity);
