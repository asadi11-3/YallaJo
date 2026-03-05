// Security.Infrastructure/Seeding/AppPermissions.cs
using Security.Contracts.Authorization;

namespace Security.Infrastructure.Seeding;

internal static class AppPermissions
{
    public static IReadOnlyList<AppPermission> All { get; } = new List<AppPermission>
    {
        // ── SystemAccess ──────────────────────────────────────────────────────
        new(AppFeatures.Role,        AppAction.Read,       AppRoleGroup.SystemAccess,       "View roles"),
        new(AppFeatures.Role,        AppAction.Create,     AppRoleGroup.SystemAccess,       "Create a new role"),
        new(AppFeatures.Role,        AppAction.Update,     AppRoleGroup.SystemAccess,       "Edit a role"),
        new(AppFeatures.Role,        AppAction.Delete,     AppRoleGroup.SystemAccess,       "Delete a role"),
        new(AppFeatures.UserRole,    AppAction.Create,     AppRoleGroup.SystemAccess,       "Assign a role to a user"),
        new(AppFeatures.UserRole,    AppAction.Delete,     AppRoleGroup.SystemAccess,       "Remove a role from a user"),
        new(AppFeatures.RoleClaim,   AppAction.Read,       AppRoleGroup.SystemAccess,       "View role claims"),
        new(AppFeatures.RoleClaim,   AppAction.Create,     AppRoleGroup.SystemAccess,       "Add a claim to a role"),
        new(AppFeatures.RoleClaim,   AppAction.Delete,     AppRoleGroup.SystemAccess,       "Remove a claim from a role"),
        new(AppFeatures.User,        AppAction.Read,       AppRoleGroup.SystemAccess,       "View any user"),
        new(AppFeatures.User,        AppAction.UpdateAny,  AppRoleGroup.SystemAccess,       "Update any user"),
        new(AppFeatures.User,        AppAction.DeleteAny,  AppRoleGroup.SystemAccess,       "Hard-delete any user"),
        new(AppFeatures.User,        AppAction.SoftDelete, AppRoleGroup.SystemAccess,       "Soft-delete any user"),
        new(AppFeatures.User,        AppAction.UpdateSelf, AppRoleGroup.SystemAccess,       "Update own profile"),
        new(AppFeatures.System,      AppAction.Update,     AppRoleGroup.SystemAccess,       "Manage system settings"),

        
    }.AsReadOnly();

    public static IReadOnlyList<string> GetPermissionsForRole(string roleName) =>
        roleName switch
        {
            AppRoles.Owner =>
                All.Select(p => p.Name).ToList(),

            AppRoles.SuperAdmin =>
                All.Where(p => !(p.Feature == AppFeatures.System && p.Action == AppAction.Update))
                   .Select(p => p.Name).ToList(),

            AppRoles.Admin =>
                All.Where(p => !(p.Feature == AppFeatures.System && p.Action == AppAction.Update)
                            && !(p.Feature == AppFeatures.User   && p.Action == AppAction.DeleteAny))
                   .Select(p => p.Name).ToList(),

            AppRoles.User =>
                All.Where(p => (p.Feature == AppFeatures.User && p.Action == AppAction.UpdateSelf) || p.IsGuest)
                   .Select(p => p.Name).ToList(),

            AppRoles.TourGuide =>
                All.Where(p => p.Group == AppRoleGroup.ContentManagement
                            && (p.Action == AppAction.Read || p.Action == AppAction.Create))
                   .Select(p => p.Name).ToList(),

            AppRoles.Guest =>
                All.Where(p => p.IsGuest).Select(p => p.Name).ToList(),

            _ => Array.Empty<string>()
        };
}
