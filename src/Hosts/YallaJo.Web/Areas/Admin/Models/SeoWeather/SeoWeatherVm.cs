// <copyright file="SeoWeatherVm.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Models.SeoWeather;

using System.ComponentModel.DataAnnotations;

public sealed class SeoWeatherVm
{
    public Guid? PlaceId { get; set; }

    public bool HasQueried { get; set; }

    public WeatherDetailVm? Weather { get; set; }

    public RefreshWeatherFormVm RefreshForm { get; set; } = new();

    public ResetBudgetFormVm BudgetForm { get; set; } = new();

    public bool Exists => Weather is not null;
}

public sealed class WeatherDetailVm
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

    public string? StaleHumanLabel { get; set; }
}

public sealed class RefreshWeatherFormVm
{
    [Required]
    [Display(Name = "Place id")]
    public Guid PlaceId { get; set; }

    [Range(-90.0, 90.0)]
    [Display(Name = "Latitude")]
    public decimal Latitude { get; set; }

    [Range(-180.0, 180.0)]
    [Display(Name = "Longitude")]
    public decimal Longitude { get; set; }
}

public sealed class ResetBudgetFormVm
{
    [DataType(DataType.Date)]
    [Display(Name = "Date (UTC, optional)")]
    public DateTime? Date { get; set; }
}
