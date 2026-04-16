using Security.Application.Caching;
using Security.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.GetUser;

public sealed record GetUserQuery(Guid UserId) : IQuery<UserDto>, ICacheableQuery
{
    public string CacheKey => SecurityCacheKeys.User(UserId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => [SecurityCacheKeys.UserTag(UserId), SecurityCacheKeys.UsersTag];
}
