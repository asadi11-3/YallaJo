// <copyright file="RefreshWeatherRequest.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.Weather.Models;

public sealed record RefreshWeatherRequest(decimal Latitude, decimal Longitude);
