using Accounts.Application.Caching;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IQuery<GetProfileResult>, ICacheableQuery
{
    public string CacheKey => AccountsCacheKeys.UserProfile(UserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => [AccountsCacheKeys.UserProfileTag(UserId)];
}
