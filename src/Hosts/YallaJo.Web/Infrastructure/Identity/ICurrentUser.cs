namespace YallaJo.Web.Infrastructure.Identity;

/// <summary>
/// Single source of truth for the current user's identity and permissions inside YallaJo.Web.
///
/// Every permission and role check in the project routes through this interface.
/// No code in the project calls User.HasClaim, User.IsInRole, or User.FindFirstValue directly.
///
/// Registered as Scoped — one instance per HTTP request.
/// Claim access is lazy: the first call resolves the ClaimsPrincipal and caches it for the
/// lifetime of the request. Subsequent calls on the same request use the cached values.
/// </summary>
public interface ICurrentUser
{
    /// <summary>True when the request carries a valid, authenticated cookie.</summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The current user's ID, or null when the request is unauthenticated.
    /// Sourced from the "sub" cookie claim.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Returns true when the user's cookie carries a "Permission" claim whose value equals
    /// <paramref name="permission"/>.  Use <see cref="WebPermission"/> constants for the value.
    /// </summary>
    bool HasPermission(string permission);

    /// <summary>
    /// Returns true when the user's cookie carries a "role" claim whose value equals
    /// <paramref name="role"/> (case-insensitive).
    /// </summary>
    bool IsInRole(string role);
}
