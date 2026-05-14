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

    Task<WeatherSnapshot> FetchAsync(Guid placeId, decimal latitude, decimal longitude, CancellationToken ct);
}

public sealed record WeatherSnapshot(
    decimal Temperature,
    decimal FeelsLike,
    int Humidity,
    decimal WindSpeed,
    int WindDirection,
    string Condition,
    string IconCode,
    decimal? UvIndex,
    string? ForecastJson);
