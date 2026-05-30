// <copyright file="GetAllFaqItemsQuery.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.FaqItem.GetAllFaqItems;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Queries.FaqItem.Common;
using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

/// <summary>
/// Lists FAQ items across all entities, paginated. Closes finding F12.3
/// (previously <c>GET /api/v1/seo/faq</c> returned 405 because no MapGet was registered
/// for the bare <c>/faq</c> path — only the per-entity variant existed).
/// </summary>
public sealed record GetAllFaqItemsQuery(
    int Page = 1,
    int PageSize = 50,
    SeoEntityType? EntityType = null,
    bool ActiveOnly = true,
    string? AcceptLanguage = null)
    : IQuery<PaginatedResult<FaqItemDto>>, ICacheableQuery
{
    // agent-context.md Gotcha #13: every query implements ICacheableQuery.
    public string CacheKey => ContentSeoCacheKeys.AllFaqs(Page, PageSize, EntityType, ActiveOnly, AcceptLanguage);

    // §5.1 paginated-list policy: 5 min absolute TTL.
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    // Coarse tag — any FaqItem mutation should invalidate every global list page.
    public IReadOnlyList<string> Tags => new[] { ContentSeoCacheKeys.TagAllFaqs };
}
