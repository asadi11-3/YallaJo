// <copyright file="NoOpWeatherProvider.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.Weather;

using ContentSeo.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Wave-4 placeholder. Reports <see cref="IsAvailable"/> as <c>false</c> when
/// no API key is configured. Wave-6 will introduce a real OpenWeatherMap-backed
/// provider with retry / circuit-breaker behaviour.
/// </summary>
internal sealed class NoOpWeatherProvider(
    IOptions<WeatherOptions> options,
    ILogger<NoOpWeatherProvider> logger) : IWeatherProvider
{
    private readonly WeatherOptions options = options.Value;

    public bool IsAvailable => false;

    public Task<WeatherSnapshot> FetchAsync(decimal latitude, decimal longitude, CancellationToken ct)
    {
        logger.LogWarning(
            "NoOpWeatherProvider.FetchAsync called for ({Lat},{Lng}) — provider='{Provider}', hasKey={HasKey}.",
            latitude,
            longitude,
            this.options.Provider,
            !string.IsNullOrWhiteSpace(this.options.ApiKey));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = Math.Clamp(this.options.ForecastDays, 1, 10);
        var daily = Enumerable.Range(0, horizon)
            .Select(offset => new DailyForecast(today.AddDays(offset), 0m, 0m, "Unavailable", string.Empty, 0))
            .ToList();
        return Task.FromResult(new WeatherSnapshot(0m, 0m, 0, 0m, 0, "Unavailable", string.Empty, null, daily));
    }
}
