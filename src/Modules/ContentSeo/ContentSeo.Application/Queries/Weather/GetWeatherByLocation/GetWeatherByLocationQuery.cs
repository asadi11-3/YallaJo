// <copyright file="GetWeatherByLocationQuery.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.Weather.GetWeatherByLocation;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Queries.Weather.Common;
using ContentSeo.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

/// <summary>
/// PDF §11: cache key = (lat rounded to 2 dp, lng rounded to 2 dp, date).
/// Nearby tours share the same cache row.
/// </summary>
public sealed record GetWeatherByLocationQuery(decimal Latitude, decimal Longitude) : IQuery<WeatherDto>, ICacheableQuery
{
    private decimal RoundedLat => WeatherCache.RoundCoordinate(Latitude);

    private decimal RoundedLng => WeatherCache.RoundCoordinate(Longitude);

    public string CacheKey => ContentSeoCacheKeys.WeatherByLocation(RoundedLat, RoundedLng, DateOnly.FromDateTime(DateTime.UtcNow));

    public TimeSpan? CacheDuration => TimeSpan.FromHours(12);

    public IReadOnlyList<string> Tags => new[]
    {
        ContentSeoCacheKeys.TagForWeatherLocation(RoundedLat, RoundedLng),
    };
}
