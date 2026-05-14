// <copyright file="RefreshWeatherResult.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Weather.RefreshWeather;

public sealed record RefreshWeatherResult(Guid PlaceId, DateTime FetchedAt, DateTime ExpiresAt);
