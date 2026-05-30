using Accounts.Application.Caching;
using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.GetAdminProviderQueue;

public sealed record GetAdminProviderQueueQuery(
    ProviderApplicationStatus? StatusFilter,
    ProviderType? TypeFilter,
    int Page,
    int PageSize)
    : IQuery<GetAdminProviderQueueResult>, ICacheableQuery
{
    public string CacheKey => AccountsCacheKeys.AdminProviderQueue
        + $":{StatusFilter}:{TypeFilter}:p{Page}:s{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
    public IReadOnlyList<string> Tags => [AccountsCacheKeys.AdminProviderQueueTag];
}
