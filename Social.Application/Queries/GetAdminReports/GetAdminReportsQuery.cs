using Social.Application.Queries.Dtos;
using Social.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.GetAdminReports;

/// <summary>Admin cursor-paginated list of submitted reports.</summary>
public sealed record GetAdminReportsQuery(
    Guid? AfterCursor = null,
    int PageSize = 20) : IQuery<ReportPageDto>, ICacheableQuery
{
    public string CacheKey => $"reports:admin:{AfterCursor}:{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [SocialCacheKeys.ReportsTag];
}
