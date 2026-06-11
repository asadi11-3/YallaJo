using Security.Application.Caching;
using Security.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.SuggestUsers;

/// <summary>
/// Suggests users whose primary email contains the query fragment (max 10).
/// Feeds the admin typeahead lookups; cached briefly under the shared users tag
/// so role/lifecycle mutations invalidate suggestions automatically.
/// </summary>
public sealed record SuggestUsersQuery(string Query) : IQuery<IReadOnlyList<UserSuggestDto>>, ICacheableQuery
{
    public string CacheKey => SecurityCacheKeys.UserSuggest(Query?.Trim().ToLowerInvariant() ?? string.Empty);

    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);

    public IReadOnlyList<string> Tags => [SecurityCacheKeys.UsersTag];
}
