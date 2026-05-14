// <copyright file="WeatherEndpoints.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.Weather;

using ContentSeo.Application.Commands.Weather.RefreshWeather;
using ContentSeo.Application.Queries.Weather.Common;
using ContentSeo.Application.Queries.Weather.GetWeather;
using ContentSeo.Contracts.Authorization;
using ContentSeo.Presentation.Endpoints.Weather.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Presentation;

internal static class WeatherEndpoints
{
    internal static void MapWeatherEndpoints(RouteGroupBuilder group)
    {
        // GET /api/v1/seo/weather/{placeId}
        group.MapGet("/weather/{placeId:guid}", async (
            Guid placeId,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWeatherQuery(placeId), ct);
            if (result.IsSuccess && result.Value is { IsStale: true })
            {
                http.Response.Headers["X-Weather-Stale"] = "true";
            }

            return result.ToApiResult();
        })
        .WithName("GetWeather")
        .WithSummary("Returns cached weather data for a place. Sets X-Weather-Stale:true header when the snapshot has expired.")
        .Produces<WeatherDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // POST /api/v1/seo/weather/refresh/{placeId}
        group.MapPost("/weather/refresh/{placeId:guid}", async (
            Guid placeId,
            RefreshWeatherRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RefreshWeatherCommand(placeId, request.Latitude, request.Longitude);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("RefreshWeather")
        .WithSummary("Force-refresh weather data for a place.")
        .Produces<RefreshWeatherResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.Weather, AppAction.Refresh));
    }
}
