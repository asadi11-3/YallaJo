using ContentBlogs.Application.Interfaces;

namespace ContentBlogs.Infrastructure.Services;

/// <summary>
/// No-op implementation of <see cref="IProviderEntitiesReadClient"/>.
/// Returns an empty set so disclosure validation always passes.
/// Replace with <c>AccountsProviderEntitiesReadClient</c> once the Accounts module
/// exposes the cross-module read endpoint (Wave 8 §9.5 Pattern B fallback).
/// </summary>
internal sealed class NullProviderEntitiesReadClient : IProviderEntitiesReadClient
{
    private static readonly IReadOnlySet<Guid> EmptySet = new HashSet<Guid>();

    public Task<IReadOnlySet<Guid>> GetOwnedEntityIdsAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(EmptySet);
}
