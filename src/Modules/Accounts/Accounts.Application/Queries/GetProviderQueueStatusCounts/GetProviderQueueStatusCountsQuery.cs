using Accounts.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.GetProviderQueueStatusCounts;

/// <summary>
/// Returns the number of provider applications per status for the admin queue's
/// counted tabs. Cached under the same tag as the queue list so every queue
/// mutation invalidates both together.
/// </summary>
public sealed record GetProviderQueueStatusCountsQuery
    : IQuery<ProviderQueueStatusCountsDto>, ICacheableQuery
{
    public string CacheKey => AccountsCacheKeys.AdminProviderQueue + ":status-counts";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
    public IReadOnlyList<string> Tags => [AccountsCacheKeys.AdminProviderQueueTag];
}
