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
        // GAP-10 (RBAC fix-code audit): Role.Delete removed. No DeleteRole command or
        // endpoint exists — roles are DEACTIVATED (PATCH .../deactivate, plan §9), never
        // hard-deleted. Seeding a permission for a non-existent capability is "invented
        // functionality"; the dangling WebPermission.Role.Delete constant is removed too.

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
        // GAP-1 (RBAC fix-code audit): the Web AuditLogsController class gate is
        // [RequirePermission(WebPermission.System.Read)] ("Permission.System.Read",
        // plan §9 line 43), but this permission was never seeded — the audit pane
        // failed closed for every role. System.Read is group SystemAccess and its
        // action is Read (not Update), so it is NOT IsOwnerOnly and NOT IsSuperAdminOnly
        // in RolePermissionMapping; the Admin/SuperAdmin/Owner full sweeps therefore
        // grant it automatically. SeedRoleClaimsAsync is idempotent (inserts missing
        // claims on boot), so no EF migration is required.
        new(SecurityFeatures.System, AppAction.Read,   PermissionGroup.SystemAccess, "View system / audit console"),
        new(SecurityFeatures.System, AppAction.Update, PermissionGroup.SystemAccess, "Manage system settings"),

        // ── Audit log ────────────────────────────────────────────────────────
        new(SecurityFeatures.AuditLog, AppAction.Read, PermissionGroup.SystemAccess, "View the admin audit log timeline"),

        // ── Ops: Outbox dead-letter management (Owner + SuperAdmin only) ──────
        // Enforced by /api/v1/ops/outbox endpoints. Restricted to the top tier
        // via RolePermissionMapping.IsOpsOnly (Admin must NOT receive these).
        new(SecurityFeatures.Outbox, AppAction.Read,   PermissionGroup.SystemAccess, "View outbox dead-letter messages"),
        new(SecurityFeatures.Outbox, AppAction.Replay, PermissionGroup.SystemAccess, "Replay an outbox dead-letter message"),
    ];
}
