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

    /// <summary>
    /// Forecast horizon requested from WeatherAPI.com. The Free plan caps the
    /// forecast at 3 days; requesting more is silently truncated by the upstream API.
    /// Raise this only on a paid plan (Starter = 7 days, Pro+ = 300 days).
    /// </summary>
    public int ForecastDays { get; set; } = 3;

    /// <summary>
    /// HTTP timeout for upstream calls, in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of upstream calls allowed per UTC day, enforced by the
    /// budget gate / pre-fetch background service. The WeatherAPI.com Free plan
    /// allows 100,000 calls per month (reset midnight UTC on the 1st); 1000/day
    /// (~30,000/month) stays comfortably under that ceiling. Going over the
    /// monthly quota stops data for the rest of the month, so keep daily * ~31
    /// below 100,000 on the Free plan.
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
