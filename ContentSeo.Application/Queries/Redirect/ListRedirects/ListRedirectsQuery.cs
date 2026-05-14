// <copyright file="ListRedirectsQuery.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.Redirect.ListRedirects;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Queries.Redirect.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

public sealed record ListRedirectsQuery(
    string? OldUrl = null,
    bool? IsActive = null,
    int? StatusCode = null,
    int Page = 1,
    int PageSize = 50)
    : IQuery<PaginatedResult<RedirectDto>>, ICacheableQuery
{
    public string CacheKey =>
        ContentSeoCacheKeys.RedirectsList(
            ContentSeoCacheKeys.Hash($"{OldUrl}|{IsActive}|{StatusCode}"),
            Page,
            PageSize);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Tags => new[] { ContentSeoCacheKeys.TagRedirectsList };
}
