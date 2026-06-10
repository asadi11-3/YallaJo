using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Queries.ListLinkedProviders;

/// <summary>
/// Lists the ACTIVE external provider links for <paramref name="UserId"/>.
/// Deliberately not cached: link/unlink mutate this set and have no cache-tag
/// invalidation wired, so a cached copy would show stale link state right after
/// the user links/unlinks — the list is tiny and the query trivial.
/// </summary>
public sealed record ListLinkedProvidersQuery(Guid UserId)
    : IQuery<IReadOnlyList<LinkedProviderDto>>;
