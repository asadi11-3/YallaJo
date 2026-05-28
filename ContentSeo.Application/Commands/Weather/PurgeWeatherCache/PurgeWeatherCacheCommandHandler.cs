namespace ContentSeo.Application.Commands.Weather.PurgeWeatherCache;

using ContentSeo.Application.Caching;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class PurgeWeatherCacheCommandHandler(
    IWeatherCacheRepository weatherCacheRepository,
    HybridCache cache,
    ILogger<PurgeWeatherCacheCommandHandler> logger)
    : ICommandHandler<PurgeWeatherCacheCommand>
{
    public async Task<Result> Handle(PurgeWeatherCacheCommand request, CancellationToken ct)
    {
        try
        {
            var entry = await weatherCacheRepository.GetByIdAsync(request.Id, ct, asNoTracking: true);
            if (entry is null || entry.IsDeleted)
            {
                return Result.Failure(new Error("WeatherCache.NotFound", $"Weather cache {request.Id} not found."), Outcome.NotFound);
            }

            var deleted = await weatherCacheRepository.ExecuteDeleteAsync(w => w.Id == request.Id, ct);
            if (deleted == 0)
            {
                return Result.Failure(new Error("WeatherCache.NotFound", $"Weather cache {request.Id} not found."), Outcome.NotFound);
            }

            if (entry.PlaceId.HasValue)
            {
                await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForWeather(entry.PlaceId.Value), ct);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForWeatherLocation(entry.RoundedLatitude, entry.RoundedLongitude), ct);
            logger.LogInformation("Purged weather cache {WeatherCacheId}", request.Id);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(new Error("WeatherCache.ConcurrencyConflict", "Weather cache was modified concurrently."), Outcome.Conflict);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
