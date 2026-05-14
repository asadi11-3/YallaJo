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

    public bool IsAvailable
    {
        get
        {
            // Provider is considered unavailable until both a non-"none" provider
            // name and a non-empty API key are configured. Wave-6 will swap in a
            // real provider that returns true when ready.
            var hasProvider = !string.IsNullOrWhiteSpace(this.options.Provider)
                && !string.Equals(this.options.Provider, "none", StringComparison.OrdinalIgnoreCase);
            var hasKey = !string.IsNullOrWhiteSpace(this.options.ApiKey);
            return hasProvider && hasKey;
        }
    }

    public Task<WeatherSnapshot> FetchAsync(Guid placeId, decimal latitude, decimal longitude, CancellationToken ct)
    {
        logger.LogWarning(
            "NoOpWeatherProvider.FetchAsync called for {PlaceId} — provider='{Provider}', hasKey={HasKey}. " +
            "Real OpenWeatherMap implementation is Wave 6.",
            placeId,
            this.options.Provider,
            !string.IsNullOrWhiteSpace(this.options.ApiKey));
        throw new InvalidOperationException("Weather provider is not configured. Real OpenWeatherMap implementation is Wave 6.");
    }
}
