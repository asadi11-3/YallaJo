namespace Security.Application.Caching;

public static class SecurityCacheKeys
{
    // ── Tag constants ─────────────────────────────────────────────────────────
    /// <summary>Tag covering ALL user list queries (ListUsers).</summary>
    public const string UsersTag = "security:users";

    /// <summary>Tag covering ALL role queries (ListRoles).</summary>
    public const string RolesTag = "security:roles";

    /// <summary>
    /// Per-user tag for GetUser queries. Used for precise single-user invalidation.
    /// The <c>:tag</c> suffix keeps tags in a separate namespace from cache keys
    /// so the two cannot collide if HybridCache providers ever share storage
    /// between key and tag indexes.
    /// </summary>
    public static string UserTag(Guid userId) => $"security:user:{userId}:tag";

    // ── Cache key builders ────────────────────────────────────────────────────
    public static string User(Guid userId) => $"security:user:{userId}";
    public static string Users(int page, int pageSize) => $"security:users:p{page}:s{pageSize}";
    public static string UserSuggest(string query) => $"security:users:suggest:{query}";
    public const string ActiveRoles = "security:roles:active";
}
