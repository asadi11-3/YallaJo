// <copyright file="GetWeatherQuery.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.Weather.GetWeather;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Queries.Weather.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record GetWeatherQuery(Guid PlaceId) : IQuery<WeatherDto>, ICacheableQuery
{
    public string CacheKey => ContentSeoCacheKeys.Weather(PlaceId);

    public TimeSpan? CacheDuration => TimeSpan.FromHours(12);

    public IReadOnlyList<string> Tags => new[] { ContentSeoCacheKeys.TagForWeather(PlaceId) };
}
