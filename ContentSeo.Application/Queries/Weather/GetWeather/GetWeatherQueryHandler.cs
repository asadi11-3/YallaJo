// <copyright file="GetWeatherQueryHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.Weather.GetWeather;

using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Application.Queries.Weather.Common;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Clock;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class GetWeatherQueryHandler(
    IWeatherCacheRepository weatherCacheRepository,
    IDateTimeProvider clock,
    ILogger<GetWeatherQueryHandler> logger)
    : IQueryHandler<GetWeatherQuery, WeatherDto>
{
    public async Task<Result<WeatherDto>> Handle(GetWeatherQuery request, CancellationToken ct)
    {
        try
        {
            var entity = await weatherCacheRepository.GetByPlaceIdNoTrackingAsync(request.PlaceId, ct);
            if (entity is null)
            {
                return Result<WeatherDto>.Failure(
                    new Error("Weather.NotFound", $"No weather data available for place {request.PlaceId}."),
                    Outcome.NotFound);
            }

            var dto = WeatherDto.From(entity, clock.UtcNow);
            logger.LogDebug("Returned weather for {PlaceId} (stale={Stale})", request.PlaceId, dto.IsStale);
            return Result<WeatherDto>.Success(dto);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<WeatherDto>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
