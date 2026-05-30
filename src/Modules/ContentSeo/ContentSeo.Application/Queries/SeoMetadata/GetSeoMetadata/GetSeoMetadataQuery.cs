// <copyright file="GetSeoMetadataQuery.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.SeoMetadata.GetSeoMetadata;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Queries.SeoMetadata.Common;
using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record GetSeoMetadataQuery(SeoEntityType EntityType, Guid EntityId, string? AcceptLanguage = null)
    : IQuery<SeoMetadataDto>, ICacheableQuery
{
    public string CacheKey => ContentSeoCacheKeys.SeoMetadata(EntityType, EntityId, AcceptLanguage);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);

    public IReadOnlyList<string> Tags => new[] { ContentSeoCacheKeys.TagForSeo(EntityType, EntityId) };
}
