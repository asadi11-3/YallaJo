using YallaJo.SharedKernel.Application.Authorization;

namespace Security.Contracts.Authorization;

/// <summary>
/// Permission catalog for the Security bounded context.
/// Registered in Security.Infrastructure DI — discovered automatically by PermissionSeeder.
/// </summary>
public sealed class SecurityPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Security";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // ── Role management ──────────────────────────────────────────────────
        new(SecurityFeatures.Role, AppAction.Read,   PermissionGroup.SystemAccess, "View roles"),
        new(SecurityFeatures.Role, AppAction.Create, PermissionGroup.SystemAccess, "Create a new role"),
        new(SecurityFeatures.Role, AppAction.Update, PermissionGroup.SystemAccess, "Edit a role"),
        new(SecurityFeatures.Role, AppAction.Delete, PermissionGroup.SystemAccess, "Delete a role"),

        // ── User-role assignment ─────────────────────────────────────────────
        new(SecurityFeatures.UserRole, AppAction.Create, PermissionGroup.SystemAccess, "Assign a role to a user"),
        new(SecurityFeatures.UserRole, AppAction.Delete, PermissionGroup.SystemAccess, "Remove a role from a user"),

        // ── Role claims ──────────────────────────────────────────────────────
        new(SecurityFeatures.RoleClaim, AppAction.Read,   PermissionGroup.SystemAccess, "View role claims"),
        new(SecurityFeatures.RoleClaim, AppAction.Create, PermissionGroup.SystemAccess, "Add a claim to a role"),
        new(SecurityFeatures.RoleClaim, AppAction.Delete, PermissionGroup.SystemAccess, "Remove a claim from a role"),

        // ── User management ──────────────────────────────────────────────────
        new(SecurityFeatures.User, AppAction.Read,       PermissionGroup.SystemAccess, "View any user"),
        new(SecurityFeatures.User, AppAction.Create,     PermissionGroup.SystemAccess, "Create a user profile"),
        new(SecurityFeatures.User, AppAction.UpdateAny,  PermissionGroup.SystemAccess, "Update any user"),
        new(SecurityFeatures.User, AppAction.DeleteAny,  PermissionGroup.SystemAccess, "Hard-delete any user"),
        new(SecurityFeatures.User, AppAction.SoftDelete, PermissionGroup.SystemAccess, "Soft-delete any user"),
        new(SecurityFeatures.User, AppAction.UpdateSelf, PermissionGroup.SystemAccess, "Update own profile"),

        // ── System settings ──────────────────────────────────────────────────
        new(SecurityFeatures.System, AppAction.Update, PermissionGroup.SystemAccess, "Manage system settings"),
    ];
}
