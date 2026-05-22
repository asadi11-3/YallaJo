using YallaJo.SharedKernel.Application.Authorization;

namespace Auth.Contracts.Authorization;

public sealed class AuthPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Auth";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new(AuthFeatures.Account, AppAction.Read,   PermissionGroup.SystemAccess, "View own auth account"),
        new(AuthFeatures.Account, AppAction.Update, PermissionGroup.SystemAccess, "Update own auth account"),

        new(AuthFeatures.Session, AppAction.Read,   PermissionGroup.SystemAccess, "View own sessions"),
        new(AuthFeatures.Session, AppAction.Delete, PermissionGroup.SystemAccess, "Revoke own sessions"),

        new(AuthFeatures.Device, AppAction.Read,   PermissionGroup.SystemAccess, "View own devices"),
        new(AuthFeatures.Device, AppAction.Update, PermissionGroup.SystemAccess, "Update own devices"),
        new(AuthFeatures.Device, AppAction.Delete, PermissionGroup.SystemAccess, "Revoke own devices"),

        new(AuthFeatures.ExternalProvider, AppAction.Create, PermissionGroup.SystemAccess, "Link external providers"),
        new(AuthFeatures.ExternalProvider, AppAction.Delete, PermissionGroup.SystemAccess, "Unlink external providers"),
    ];
}
