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
    IWeatherBudgetGate budgetGate,
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

            // PDF §11: cache key = (lat rounded to 2 dp, lng rounded to 2 dp, date).
            var roundedLat = DomainWeatherCache.RoundCoordinate(request.Latitude);
            var roundedLng = DomainWeatherCache.RoundCoordinate(request.Longitude);
            var now = clock.UtcNow;
            var forecastDate = DateOnly.FromDateTime(now);
            var expiresAt = now.AddHours(12);

            if (!await budgetGate.TryConsumeAsync(ct))
            {
                return Result<RefreshWeatherResult>.Failure(
                    new Error("Weather.BudgetExhausted", "Daily weather API budget exhausted."),
                    Outcome.TooManyRequests);
            }

            var snapshot = await provider.FetchAsync(request.Latitude, request.Longitude, ct);

            // Forecast horizon is plan-dependent (WeatherAPI.com free = 3 days, paid = up to 365).
            // Only an empty forecast is an actual error; a shorter-than-requested horizon is valid.
            if (snapshot.DailyForecasts.Count == 0)
            {
                logger.LogWarning(
                    "Weather provider returned no daily forecasts. PlaceId={PlaceId}",
                    request.PlaceId);
            }

            var forecastJson = System.Text.Json.JsonSerializer.Serialize(snapshot.DailyForecasts);

            var entity = await weatherCacheRepository.GetByLocationAsync(roundedLat, roundedLng, forecastDate, ct);
            if (entity is null)
            {
                entity = DomainWeatherCache.Create(
                    request.Latitude,
                    request.Longitude,
                    forecastDate,
                    request.PlaceId,
                    snapshot.Temperature,
                    snapshot.FeelsLike,
                    snapshot.Humidity,
                    snapshot.WindSpeed,
                    snapshot.WindDirection,
                    snapshot.Condition,
                    snapshot.IconCode,
                    snapshot.UvIndex,
                    forecastJson,
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
                    forecastJson,
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

            // Invalidate both old PlaceId-based tag and new location-based tag.
            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForWeather(request.PlaceId), ct);
            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForWeatherLocation(roundedLat, roundedLng), ct);
            logger.LogInformation(
                "Refreshed weather for PlaceId={PlaceId} at ({Lat},{Lng})",
                request.PlaceId, roundedLat, roundedLng);

            return Result<RefreshWeatherResult>.Success(new RefreshWeatherResult(request.PlaceId, now, expiresAt));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<RefreshWeatherResult>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Weather provider failed for PlaceId={PlaceId}", request.PlaceId);
            return Result<RefreshWeatherResult>.Failure(
                new Error("Weather.UpstreamUnavailable", "Weather provider could not return data."),
                Outcome.ServerError);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Weather provider HTTP failure for PlaceId={PlaceId}", request.PlaceId);
            return Result<RefreshWeatherResult>.Failure(
                new Error("Weather.UpstreamUnavailable", "Weather provider could not return data."),
                Outcome.ServerError);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Weather provider timed out for PlaceId={PlaceId}", request.PlaceId);
            return Result<RefreshWeatherResult>.Failure(
                new Error("Weather.UpstreamTimeout", "Weather provider timed out."),
                Outcome.ServerError);
        }
    }
}
