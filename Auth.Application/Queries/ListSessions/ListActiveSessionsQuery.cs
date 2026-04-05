using Auth.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Queries.ListSessions;

public sealed record ListActiveSessionsQuery(Guid UserId) : IQuery<IReadOnlyList<ActiveSessionDto>>, ICacheableQuery
{
    public string CacheKey => AuthCacheKeys.UserSessions(UserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => [AuthCacheKeys.UserSessionsTag(UserId)];
}
