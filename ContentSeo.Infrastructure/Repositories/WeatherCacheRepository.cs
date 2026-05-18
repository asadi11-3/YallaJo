// <copyright file="WeatherCacheRepository.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Domain.Entities;
using ContentSeo.Infrastructure.Persistence;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace ContentSeo.Infrastructure.Repositories;

internal sealed class WeatherCacheRepository(ContentSeoDbContext context)
    : EfRepository<WeatherCache, Guid>(context), IWeatherCacheRepository
{
    public async Task<WeatherCache?> GetByPlaceIdAsync(Guid placeId, CancellationToken ct = default)
        => await GetAsync(w => w.PlaceId == placeId && !w.IsDeleted, asNoTracking: false, ct: ct);

    public async Task<WeatherCache?> GetByPlaceIdNoTrackingAsync(Guid placeId, CancellationToken ct = default)
        => await GetAsync(w => w.PlaceId == placeId && !w.IsDeleted, asNoTracking: true, ct: ct);

    /// <inheritdoc/>
    public async Task<WeatherCache?> GetByLocationAsync(
        decimal roundedLat,
        decimal roundedLng,
        DateOnly date,
        CancellationToken ct = default)
        => await GetAsync(
            w => w.RoundedLatitude == roundedLat
              && w.RoundedLongitude == roundedLng
              && w.ForecastDate == date
              && !w.IsDeleted,
            asNoTracking: false,
            ct: ct);

    /// <inheritdoc/>
    public async Task<WeatherCache?> GetByLocationNoTrackingAsync(
        decimal roundedLat,
        decimal roundedLng,
        DateOnly date,
        CancellationToken ct = default)
        => await GetAsync(
            w => w.RoundedLatitude == roundedLat
              && w.RoundedLongitude == roundedLng
              && w.ForecastDate == date
              && !w.IsDeleted,
            asNoTracking: true,
            ct: ct);
}
