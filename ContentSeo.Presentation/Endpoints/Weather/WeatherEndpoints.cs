// <copyright file="WeatherEndpoints.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.Weather;

using ContentSeo.Application.Commands.Weather.RefreshWeather;
using ContentSeo.Application.Commands.Weather.PurgeWeatherCache;
using ContentSeo.Application.Commands.Weather.ResetWeatherBudget;
using ContentSeo.Application.Queries.Weather.Common;
using ContentSeo.Application.Queries.Weather.GetWeather;
using ContentSeo.Application.Queries.Weather.GetWeatherByLocation;
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

        // GET /api/v1/seo/weather?lat=&lng=  (PDF §11 coordinate-based cache key)
        group.MapGet("/weather", async (
            decimal lat,
            decimal lng,
            HttpContext http,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetWeatherByLocationQuery(lat, lng), ct);
            if (result.IsSuccess && result.Value is { IsStale: true })
            {
                http.Response.Headers["X-Weather-Stale"] = "true";
            }

            return result.ToApiResult();
        })
        .WithName("GetWeatherByLocation")
        .WithSummary("Returns cached weather data for a location (lat/lng). PDF §11: cache key = (lat-rounded-2dp, lng-rounded-2dp, date).")
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

        // DELETE /api/v1/seo/weather/cache/{id}
        group.MapDelete("/weather/cache/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new PurgeWeatherCacheCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("PurgeWeatherCache")
        .WithSummary("Delete a weather cache entry.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.Weather, AppAction.Delete));

        // PUT /api/v1/seo/weather/budget/reset
        group.MapPut("/weather/budget/reset", async (
            DateOnly? date,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ResetWeatherBudgetCommand(date), ct);
            return result.ToApiResult();
        })
        .WithName("ResetWeatherBudget")
        .WithSummary("Reset the weather API daily budget counter for a UTC date (defaults to today).")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentSeoFeatures.Weather, AppAction.Update));
    }
}
