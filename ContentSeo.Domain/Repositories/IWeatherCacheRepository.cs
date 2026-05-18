// <copyright file="IWeatherCacheRepository.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

using ContentSeo.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Domain.Repositories;

public interface IWeatherCacheRepository : IRepository<WeatherCache>
{
    /// <summary>
    /// Returns the cache row for the given place (tracked, with mutation intent).
    /// Kept for backward compatibility with existing RefreshWeather command.
    /// </summary>
    Task<WeatherCache?> GetByPlaceIdAsync(Guid placeId, CancellationToken ct = default);

    /// <summary>Returns the cache row for the given place (untracked, for reads).</summary>
    Task<WeatherCache?> GetByPlaceIdNoTrackingAsync(Guid placeId, CancellationToken ct = default);

    /// <summary>
    /// PDF §11: cache key = (lat rounded to 2 dp, lng rounded to 2 dp, date).
    /// Returns the cache row for the given rounded coordinates + date (tracked).
    /// </summary>
    Task<WeatherCache?> GetByLocationAsync(decimal roundedLat, decimal roundedLng, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// PDF §11: cache key = (lat rounded to 2 dp, lng rounded to 2 dp, date).
    /// Returns the cache row for the given rounded coordinates + date (untracked, for reads).
    /// </summary>
    Task<WeatherCache?> GetByLocationNoTrackingAsync(decimal roundedLat, decimal roundedLng, DateOnly date, CancellationToken ct = default);
}
