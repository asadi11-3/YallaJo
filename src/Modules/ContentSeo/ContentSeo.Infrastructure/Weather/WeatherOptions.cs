// <copyright file="WeatherOptions.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.Weather;

/// <summary>
/// Configuration bound from the <c>Weather</c> section of appsettings.
/// WeatherAPI.com configuration. Falls back to NoOp when Provider is "none" or ApiKey is blank.
/// </summary>
public sealed class WeatherOptions
{
    public const string SectionName = "Weather";

    /// <summary>
    /// Upstream weather provider name (e.g. "weatherapi", "none"). When set
    /// to "none" or left blank, the NoOp provider is used regardless of other settings.
    /// </summary>
    public string Provider { get; set; } = "none";

    /// <summary>
    /// API key for the upstream provider. Empty in dev — supply via user-secrets
    /// or environment variables in production. The NoOp provider reports
    /// <see cref="ContentSeo.Application.Interfaces.IWeatherProvider.IsAvailable"/>
    /// as <c>false</c> while this is blank.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the upstream provider's REST API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.weatherapi.com/v1";

    /// <summary>Forecast horizon requested from WeatherAPI.com. YallaJo requires 7 days.</summary>
    public int ForecastDays { get; set; } = 7;

    /// <summary>
    /// HTTP timeout for upstream calls, in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of upstream calls allowed per UTC day, enforced by the
    /// pre-fetch background service. Defaults to 1000 (matches free-tier ceiling).
    /// </summary>
    public int DailyBudget { get; set; } = 1000;

    /// <summary>
    /// Cache TTL for a successful weather fetch. Mirrors the GetWeatherQuery
    /// HybridCache duration so the upstream call rate stays bounded.
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(12);

    /// <summary>
    /// UTC hour at which <c>WeatherPreFetchService</c> runs daily (0–23). Default 5.
    /// </summary>
    public int PreFetchHourUtc { get; set; } = 5;
}
