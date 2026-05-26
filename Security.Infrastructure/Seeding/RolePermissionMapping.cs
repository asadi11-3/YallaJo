using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;

namespace Security.Infrastructure.Seeding;

/// <summary>
/// Maps roles to permissions by querying all registered <see cref="IPermissionCatalog"/> instances.
/// Replaces the static switch in <c>AppPermissions.GetPermissionsForRole</c>.
/// Adding a new module's catalog to DI automatically adds its permissions here.
/// </summary>
public sealed class RolePermissionMapping
{
    private readonly IReadOnlyList<PermissionDescriptor> _all;

    public RolePermissionMapping(IEnumerable<IPermissionCatalog> catalogs)
    {
        _all = catalogs.SelectMany(c => c.Permissions).ToList();
    }

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) =>
        roleName switch
        {
            AppRoles.Owner =>
                _all.Select(p => p.Name).ToList(),

            AppRoles.SuperAdmin =>
                _all.Where(p => !IsOwnerOnly(p))
                    .Select(p => p.Name).ToList(),

            AppRoles.Admin =>
                _all.Where(p => !IsOwnerOnly(p) && !IsSuperAdminOnly(p))
                    .Select(p => p.Name).ToList(),

            AppRoles.Provider =>
                _all.Where(p => (p.Group == PermissionGroup.ContentManagement
                                 && p.Action is AppAction.Read or AppAction.Create
                                     or AppAction.Update or AppAction.Delete)
                             || (p.Feature == SecurityFeatures.User && p.Action == AppAction.UpdateSelf)
                             || p.IsGuestAccessible)
                    .Select(p => p.Name).ToList(),

            AppRoles.Creator =>
                _all.Where(p => (p.Group == PermissionGroup.ContentManagement
                                 && p.Action is AppAction.Read or AppAction.Create or AppAction.Delete)
                             || (p.Feature == SecurityFeatures.User && p.Action == AppAction.UpdateSelf)
                             || p.IsGuestAccessible)
                    .Select(p => p.Name).ToList(),

            AppRoles.User =>
                _all.Where(p => (p.Feature == SecurityFeatures.User && p.Action == AppAction.UpdateSelf)
                             || p.IsGuestAccessible)
                    .Select(p => p.Name).ToList(),

            AppRoles.TourGuide =>
                _all.Where(p => (p.Group == PermissionGroup.ContentManagement
                                 && (p.Action == AppAction.Read || p.Action == AppAction.Create || p.Action == AppAction.Delete))
                             || (p.Feature == SecurityFeatures.User && p.Action == AppAction.UpdateSelf)
                             || p.IsGuestAccessible)
                    .Select(p => p.Name).ToList(),

            AppRoles.Guest =>
                _all.Where(p => p.IsGuestAccessible)
                    .Select(p => p.Name).ToList(),

            _ => []
        };

    private static bool IsOwnerOnly(PermissionDescriptor p) =>
        p.Feature == SecurityFeatures.System && p.Action == AppAction.Update;

    private static bool IsSuperAdminOnly(PermissionDescriptor p) =>
        p.Feature == SecurityFeatures.User && p.Action == AppAction.DeleteAny;
}
