namespace YallaJo.Web.Features.AdminNav;

/// <summary>
/// View model backing the admin sidebar (AdminNavViewComponent). Carries the
/// DB-backed roles and permission snapshot from GET /security/me. When <see cref="IsDbBacked"/>
/// is false the snapshot could not be loaded and the sidebar degrades gracefully
/// (ERR3) by falling back to the request's JWT-claim permissions and roles.
/// </summary>
public sealed class AdminNavVm
{
    /// <summary>DB-backed role names (e.g. "Admin", "SuperAdmin"). Empty when not DB-backed.</summary>
    public IReadOnlyCollection<string> Roles { get; init; } = [];

    /// <summary>DB-backed permission values (e.g. "Permission.User.Read"). Empty when not DB-backed.</summary>
    public IReadOnlyCollection<string> Permissions { get; init; } = [];

    /// <summary>True when the snapshot was loaded from GET /security/me; false on any failure.</summary>
    public bool IsDbBacked { get; init; }
}
