namespace Auth.Application.Caching;

/// <summary>
/// Single source of truth for all cache-key and tag patterns used in the Auth module.
/// Shared between query handlers (writers) and command handlers (invalidators)
/// to guarantee perfect key symmetry.
/// <para>
/// Cache <b>keys</b> and <b>tags</b> live in separate string namespaces — keys
/// identify a single cache entry, tags group multiple entries for bulk
/// invalidation. The <c>:tag</c> suffix on tag builders keeps the two
/// namespaces from colliding even if a HybridCache provider ever shares
/// storage between key and tag indexes.
/// </para>
/// </summary>
public static class AuthCacheKeys
{
    // ── Sessions ─────────────────────────────────────────────────────────────

    /// <summary>Cache entry key for the active-sessions list of one user.</summary>
    public static string UserSessions(Guid userId) => $"auth:sessions:{userId}";

    /// <summary>
    /// Tag assigned to every sessions cache entry for a user.
    /// Pass to <c>HybridCache.RemoveByTagAsync</c> from any command that
    /// mutates session state (Logout, RevokeSession, LogoutAll, ForceRevoke, etc.).
    /// The <c>:tag</c> suffix keeps the tag namespace separate from the key
    /// namespace produced by <see cref="UserSessions"/>.
    /// </summary>
    public static string UserSessionsTag(Guid userId) => $"auth:sessions:{userId}:tag";
}
