// <copyright file="RefreshWeatherCommandHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Weather.RefreshWeather;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

using DomainWeatherCache = ContentSeo.Domain.Entities.WeatherCache;

public sealed class RefreshWeatherCommandHandler(
    IWeatherCacheRepository weatherCacheRepository,
    IWeatherProvider provider,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    IDateTimeProvider clock,
    ILogger<RefreshWeatherCommandHandler> logger)
    : ICommandHandler<RefreshWeatherCommand, RefreshWeatherResult>
{
    public async Task<Result<RefreshWeatherResult>> Handle(RefreshWeatherCommand request, CancellationToken ct)
    {
        try
        {
            // Auth handled by endpoint MustHavePermissionAttribute.

            if (!provider.IsAvailable)
            {
                return Result<RefreshWeatherResult>.Failure(
                    new Error("Weather.UpstreamUnavailable", "Weather provider is not configured."),
                    Outcome.ServerError);
            }

            var snapshot = await provider.FetchAsync(request.PlaceId, request.Latitude, request.Longitude, ct);
            var now = clock.UtcNow;
            var expiresAt = now.AddHours(12);

            var entity = await weatherCacheRepository.GetByPlaceIdAsync(request.PlaceId, ct);
            if (entity is null)
            {
                entity = DomainWeatherCache.Create(
                    request.PlaceId,
                    snapshot.Temperature,
                    snapshot.FeelsLike,
                    snapshot.Humidity,
                    snapshot.WindSpeed,
                    snapshot.WindDirection,
                    snapshot.Condition,
                    snapshot.IconCode,
                    snapshot.UvIndex,
                    snapshot.ForecastJson,
                    now,
                    expiresAt);
                await weatherCacheRepository.AddAsync(entity, ct);
            }
            else
            {
                entity.Refresh(
                    snapshot.Temperature,
                    snapshot.FeelsLike,
                    snapshot.Humidity,
                    snapshot.WindSpeed,
                    snapshot.WindDirection,
                    snapshot.Condition,
                    snapshot.IconCode,
                    snapshot.UvIndex,
                    snapshot.ForecastJson,
                    now,
                    expiresAt);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<RefreshWeatherResult>.Failure(
                    new Error("Weather.ConcurrencyConflict", "Weather cache was modified concurrently."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForWeather(request.PlaceId), ct);
            logger.LogInformation("Refreshed weather for {PlaceId}", request.PlaceId);

            return Result<RefreshWeatherResult>.Success(new RefreshWeatherResult(request.PlaceId, now, expiresAt));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<RefreshWeatherResult>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
