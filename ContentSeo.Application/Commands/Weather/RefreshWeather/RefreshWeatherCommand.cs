// <copyright file="RefreshWeatherCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Weather.RefreshWeather;

using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record RefreshWeatherCommand(Guid PlaceId, decimal Latitude, decimal Longitude) : ICommand<RefreshWeatherResult>;
