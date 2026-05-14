// <copyright file="IWeatherCacheRepository.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

using ContentSeo.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Application.Interfaces;

public interface IWeatherCacheRepository : IRepository<WeatherCache>
{
    /// <summary>Returns the cache row for the given place (tracked, with mutation intent).</summary>
    Task<WeatherCache?> GetByPlaceIdAsync(Guid placeId, CancellationToken ct = default);

    /// <summary>Returns the cache row for the given place (untracked, for reads).</summary>
    Task<WeatherCache?> GetByPlaceIdNoTrackingAsync(Guid placeId, CancellationToken ct = default);
}
