// <copyright file="WeatherDto.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.Weather.Common;

using ContentSeo.Domain.Entities;

public sealed record WeatherDto(
    Guid PlaceId,
    decimal? Temperature,
    decimal? FeelsLike,
    int? Humidity,
    decimal? WindSpeed,
    int? WindDirection,
    string? Condition,
    string? Icon,
    decimal? UvIndex,
    DateTime FetchedAt,
    DateTime ExpiresAt,
    bool IsStale)
{
    public static WeatherDto From(WeatherCache cache, DateTime nowUtc) => new(
        cache.PlaceId,
        cache.Temperature,
        cache.FeelsLike,
        cache.Humidity,
        cache.WindSpeed,
        cache.WindDirection,
        cache.Condition,
        cache.Icon,
        cache.UvIndex,
        cache.FetchedAt,
        cache.ExpiresAt,
        cache.IsStale(nowUtc));
}
