namespace ContentSeo.Application.Queries.Sitemap.ListSitemapEntries;

using ContentSeo.Application.Queries.Sitemap.Common;
using ContentSeo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class ListSitemapEntriesQueryHandler(
    ISitemapEntryRepository sitemapEntryRepository,
    ILogger<ListSitemapEntriesQueryHandler> logger)
    : IQueryHandler<ListSitemapEntriesQuery, PaginatedResult<SitemapEntryDto>>
{
    public async Task<Result<PaginatedResult<SitemapEntryDto>>> Handle(ListSitemapEntriesQuery request, CancellationToken ct)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 200);
            var entityType = string.IsNullOrWhiteSpace(request.EntityType) ? null : request.EntityType.Trim();

            var entries = await sitemapEntryRepository.GetAllAsync(
                filter: s => !s.IsDeleted
                    && (entityType == null || s.EntityType == entityType)
                    && (request.IsActive == null || s.IsActive == request.IsActive),
                orderBy: q => q.OrderBy(s => s.EntityType).ThenBy(s => s.Url),
                asNoTracking: true,
                ct: ct);

            var total = entries.Count;
            var items = entries.Skip((page - 1) * pageSize).Take(pageSize).Select(SitemapEntryDto.From).ToList();
            logger.LogDebug("Listed {Count} sitemap entries (total={Total}, page={Page})", items.Count, total, page);
            return Result<PaginatedResult<SitemapEntryDto>>.Success(new PaginatedResult<SitemapEntryDto>(items, total, page, pageSize));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<PaginatedResult<SitemapEntryDto>>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
