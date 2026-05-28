using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Creator.AdminListApplications;

public sealed record AdminListCreatorApplicationsQuery(
    int Page = 1,
    int PageSize = 20,
    CreatorApplicationStatus? Status = null,
    string? Search = null)
    : IQuery<PaginatedResult<CreatorApplicationSummaryDto>>, ICacheableQuery
{
    public string CacheKey
    {
        get
        {
            var filter = $"status:{Status?.ToString() ?? "any"}" +
                         $":search:{NormalizeSearch(Search)}";
            return ContentBlogsCacheKeys.CreatorApplicationList(filter, Page, PageSize);
        }
    }

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(3);

    public IReadOnlyList<string> Tags => [ContentBlogsCacheKeys.CreatorApplicationsListTag];

    private static string NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim().ToLowerInvariant();
}
