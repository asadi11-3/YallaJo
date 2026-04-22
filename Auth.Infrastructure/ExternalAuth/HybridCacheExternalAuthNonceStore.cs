using Auth.Application.ExternalAuth;
using Microsoft.Extensions.Caching.Hybrid;

namespace Auth.Infrastructure.ExternalAuth;

/// <summary>
/// Distributed one-time-nonce store backed by <see cref="HybridCache"/>.
///
/// <para>
/// Uses a create-or-fail pattern: <see cref="HybridCache.GetOrCreateAsync"/>
/// atomically inserts the marker value on the first call and returns the
/// existing value on subsequent calls (detected via a unique sentinel per
/// invocation). If the sentinel we wrote is not the value that comes back, the
/// key was already consumed → we return <c>false</c> so the caller rejects the
/// ticket.
/// </para>
///
/// <para>
/// Expiration is bounded by the ticket's own <c>ExpiresAt</c> so memory is
/// reclaimed automatically once the ticket could no longer be replayed anyway.
/// </para>
/// </summary>
internal sealed class HybridCacheExternalAuthNonceStore : IExternalAuthNonceStore
{
    private const string KeyPrefix = "ext-auth-nonce:";
    private readonly HybridCache _cache;

    public HybridCacheExternalAuthNonceStore(HybridCache cache) => _cache = cache;

    public async Task<bool> TryConsumeAsync(Guid ticketId, DateTime expiresAt, CancellationToken ct = default)
    {
        // If the ticket is already past its own expiry we still want to reject it
        // — but the verifier catches that before reaching us. Defensive floor.
        var ttl = expiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
            ttl = TimeSpan.FromSeconds(60);

        var key = KeyPrefix + ticketId.ToString("N");
        var ourSentinel = Guid.NewGuid().ToString("N");

        var observed = await _cache.GetOrCreateAsync(
            key,
            factory: (_) => ValueTask.FromResult(ourSentinel),
            options: new HybridCacheEntryOptions
            {
                Expiration = ttl,
                LocalCacheExpiration = ttl,
            },
            tags: null,
            cancellationToken: ct).ConfigureAwait(false);

        // If the value we saw back is the sentinel we just minted, we were the
        // first writer — the nonce is freshly consumed. Otherwise someone else
        // already consumed it.
        return string.Equals(observed, ourSentinel, StringComparison.Ordinal);
    }
}
