// <copyright file="GetWeatherByLocationQueryHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.Weather.GetWeatherByLocation;

using ContentSeo.Application.Queries.Weather.Common;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class GetWeatherByLocationQueryHandler(
    IWeatherCacheRepository weatherCacheRepository,
    IDateTimeProvider clock,
    ILogger<GetWeatherByLocationQueryHandler> logger)
    : IQueryHandler<GetWeatherByLocationQuery, WeatherDto>
{
    public async Task<Result<WeatherDto>> Handle(GetWeatherByLocationQuery request, CancellationToken ct)
    {
        try
        {
            var roundedLat = WeatherCache.RoundCoordinate(request.Latitude);
            var roundedLng = WeatherCache.RoundCoordinate(request.Longitude);
            var today = DateOnly.FromDateTime(clock.UtcNow);

            var entity = await weatherCacheRepository.GetByLocationNoTrackingAsync(roundedLat, roundedLng, today, ct);
            if (entity is null)
            {
                return Result<WeatherDto>.Failure(
                    new Error("Weather.NotFound", $"No weather data available for location ({roundedLat},{roundedLng})."),
                    Outcome.NotFound);
            }

            var dto = WeatherDto.From(entity, clock.UtcNow);
            logger.LogDebug(
                "Returned weather for ({Lat},{Lng}) (stale={Stale})",
                roundedLat, roundedLng, dto.IsStale);
            return Result<WeatherDto>.Success(dto);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<WeatherDto>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
