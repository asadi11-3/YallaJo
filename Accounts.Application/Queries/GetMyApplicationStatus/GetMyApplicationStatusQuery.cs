using Accounts.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.GetMyApplicationStatus;

public sealed record GetMyApplicationStatusQuery(Guid UserId)
    : IQuery<GetMyApplicationStatusResult>, ICacheableQuery
{
    public string CacheKey => AccountsCacheKeys.MyApplicationStatus(UserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [AccountsCacheKeys.MyApplicationStatusTag(UserId)];
}
