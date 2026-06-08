using Security.Application.Caching;
using Security.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.ListUsers;

public sealed record ListUsersQuery : IQuery<PagedUsersResponse>, ICacheableQuery
{
    public int Page { get; }
    public int PageSize { get; }

    public ListUsersQuery(int page = 1, int pageSize = 20)
    {
        Page = page < 1 ? 1 : page;
        PageSize = Math.Clamp(pageSize, 1, 50);
    }

    public string CacheKey => SecurityCacheKeys.Users(Page, PageSize);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => [SecurityCacheKeys.UsersTag];
}
