// <copyright file="SeoWeatherRequest.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Models.SeoWeather;

public sealed class WeatherLookupRequest
{
    public Guid? PlaceId { get; set; }
}

public sealed record RefreshWeatherApiRequest(decimal Latitude, decimal Longitude);
