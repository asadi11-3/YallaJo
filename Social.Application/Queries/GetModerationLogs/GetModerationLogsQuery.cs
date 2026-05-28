using Social.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Queries.GetModerationLogs;

/// <summary>Admin cursor-paginated list of moderation log entries.</summary>
public sealed record GetModerationLogsQuery(
    Guid? AfterCursor = null,
    int PageSize = 20) : IQuery<ModerationLogPageDto>, ICacheableQuery
{
    public string CacheKey => $"moderation:logs:{AfterCursor}:{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["moderation:logs"];
}
