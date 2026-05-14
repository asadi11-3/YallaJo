// <copyright file="NoOpWeatherProvider.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.Weather;

using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.Extensions.Logging;

internal sealed class NoOpWeatherProvider(ILogger<NoOpWeatherProvider> logger) : IWeatherProvider
{
    public bool IsAvailable => false;

    public Task<WeatherSnapshot> FetchAsync(Guid placeId, decimal latitude, decimal longitude, CancellationToken ct)
    {
        logger.LogWarning("NoOpWeatherProvider.FetchAsync called for {PlaceId} — no upstream configured.", placeId);
        throw new InvalidOperationException("Weather provider is not configured. Real OpenWeatherMap implementation is Wave 6.");
    }
}
