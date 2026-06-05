// <copyright file="SeoWeatherResponse.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Models.SeoWeather;

public sealed class WeatherItemResponse
{
    public Guid? PlaceId { get; set; }

    public decimal? Temperature { get; set; }

    public decimal? FeelsLike { get; set; }

    public int? Humidity { get; set; }

    public decimal? WindSpeed { get; set; }

    public int? WindDirection { get; set; }

    public string? Condition { get; set; }

    public string? Icon { get; set; }

    public decimal? UvIndex { get; set; }

    public DateTime FetchedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsStale { get; set; }

    public DateTime? StaleSince { get; set; }

    public string? StaleHumanLabel { get; set; }
}

public sealed class RefreshWeatherResponse
{
    public Guid PlaceId { get; set; }

    public DateTime FetchedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
