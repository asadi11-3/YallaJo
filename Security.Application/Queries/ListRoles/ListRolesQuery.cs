using Security.Application.Caching;
using Security.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.ListRoles;

public sealed record ListRolesQuery : IQuery<IReadOnlyList<RoleDto>>, ICacheableQuery
{
    public string CacheKey => SecurityCacheKeys.ActiveRoles;
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(60);
    public IReadOnlyList<string> Tags => [SecurityCacheKeys.RolesTag];
}
