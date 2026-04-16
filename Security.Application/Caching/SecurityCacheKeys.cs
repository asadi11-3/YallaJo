namespace Security.Application.Caching;

public static class SecurityCacheKeys
{
    // ── Tag constants ─────────────────────────────────────────────────────────
    /// <summary>Tag covering ALL user list queries (ListUsers).</summary>
    public const string UsersTag = "security:users";

    /// <summary>Tag covering ALL role queries (ListRoles).</summary>
    public const string RolesTag = "security:roles";

    /// <summary>Per-user tag for GetUser queries. Used for precise single-user invalidation.</summary>
    public static string UserTag(Guid userId) => $"security:user:{userId}";

    // ── Cache key builders ────────────────────────────────────────────────────
    public static string User(Guid userId) => $"security:user:{userId}";
    public static string Users(int page, int pageSize) => $"security:users:p{page}:s{pageSize}";
    public const string ActiveRoles = "security:roles:active";
}
