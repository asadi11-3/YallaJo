// <copyright file="RefreshWeatherCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Weather.RefreshWeather;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

using DomainWeatherCache = ContentSeo.Domain.Entities.WeatherCache;

public sealed record RefreshWeatherCommand(Guid PlaceId, decimal Latitude, decimal Longitude) : ICommand<RefreshWeatherResult>;

public sealed record RefreshWeatherResult(Guid PlaceId, DateTime FetchedAt, DateTime ExpiresAt);

public sealed class RefreshWeatherCommandHandler(
    IWeatherCacheRepository weatherCacheRepository,
    IWeatherProvider provider,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    ILogger<RefreshWeatherCommandHandler> logger)
    : ICommandHandler<RefreshWeatherCommand, RefreshWeatherResult>
{
    public async Task<Result<RefreshWeatherResult>> Handle(RefreshWeatherCommand request, CancellationToken ct)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result<RefreshWeatherResult>.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);
            }

            if (!provider.IsAvailable)
            {
                return Result<RefreshWeatherResult>.Failure(
                    new Error("Weather.UpstreamUnavailable", "Weather provider is not configured."),
                    Outcome.ServerError);
            }

            WeatherSnapshot snapshot;
            try
            {
                snapshot = await provider.FetchAsync(request.PlaceId, request.Latitude, request.Longitude, ct);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogWarning(ex, "Weather provider failure for {PlaceId}", request.PlaceId);
                return Result<RefreshWeatherResult>.Failure(
                    new Error("Weather.UpstreamUnavailable", "Weather provider is unavailable."),
                    Outcome.ServerError);
            }

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
