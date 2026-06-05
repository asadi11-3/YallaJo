// <copyright file="SeoWeatherMapper.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Models.SeoWeather;

public static class SeoWeatherMapper
{
    public static WeatherDetailVm ToDetail(WeatherItemResponse response)
    {
        return new WeatherDetailVm
        {
            PlaceId = response.PlaceId,
            Temperature = response.Temperature,
            FeelsLike = response.FeelsLike,
            Humidity = response.Humidity,
            WindSpeed = response.WindSpeed,
            WindDirection = response.WindDirection,
            Condition = response.Condition,
            Icon = response.Icon,
            UvIndex = response.UvIndex,
            FetchedAt = response.FetchedAt,
            ExpiresAt = response.ExpiresAt,
            IsStale = response.IsStale,
            StaleHumanLabel = response.StaleHumanLabel,
        };
    }
}
