namespace Auth.Application.Caching;

/// <summary>
/// Single source of truth for all cache-key and tag patterns used in the Auth module.
/// Shared between query handlers (writers) and command handlers (invalidators)
/// to guarantee perfect key symmetry.
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
    /// </summary>
    public static string UserSessionsTag(Guid userId) => $"auth:sessions:{userId}";
}
