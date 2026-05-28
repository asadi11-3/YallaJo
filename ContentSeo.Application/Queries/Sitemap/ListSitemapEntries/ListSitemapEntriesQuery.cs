using ContentSeo.Application.Queries.Sitemap.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentSeo.Application.Queries.Sitemap.ListSitemapEntries;

public sealed record ListSitemapEntriesQuery(
    string? EntityType,
    bool? IsActive,
    int Page,
    int PageSize) : IQuery<PaginatedResult<SitemapEntryDto>>;
