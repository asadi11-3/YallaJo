// <copyright file="WeatherDto.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.Weather.Common;

using ContentSeo.Domain.Entities;

public sealed record WeatherDto(
    Guid? PlaceId,
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
    bool IsStale,
    /// <summary>
    /// PDF §11: when stale, the UTC timestamp at which the cache expired.
    /// Null when not stale.
    /// </summary>
    DateTime? StaleSince,
    /// <summary>
    /// PDF §11: human-readable "Last updated X hours ago" label shown when stale.
    /// Null when not stale.
    /// </summary>
    string? StaleHumanLabel)
{
    public static WeatherDto From(WeatherCache cache, DateTime nowUtc)
    {
        var isStale = cache.IsStale(nowUtc);
        DateTime? staleSince = isStale ? cache.ExpiresAt : null;
        string? staleLabel = null;
        if (isStale)
        {
            var hoursAgo = (int)Math.Floor((nowUtc - cache.FetchedAt).TotalHours);
            staleLabel = hoursAgo <= 1
                ? "Last updated 1 hour ago"
                : $"Last updated {hoursAgo} hours ago";
        }

        return new WeatherDto(
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
            isStale,
            staleSince,
            staleLabel);
    }
}
