// <copyright file="GetFaqItemsQuery.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.FaqItem.GetFaqItems;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Queries.FaqItem.Common;
using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record GetFaqItemsQuery(
    SeoEntityType EntityType,
    Guid EntityId,
    string? AcceptLanguage = null)
    : IQuery<IReadOnlyList<FaqItemDto>>, ICacheableQuery
{
    public string CacheKey => ContentSeoCacheKeys.FaqList(EntityType, EntityId, AcceptLanguage);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);

    public IReadOnlyList<string> Tags => new[] { ContentSeoCacheKeys.TagForFaq(EntityType, EntityId) };
}
