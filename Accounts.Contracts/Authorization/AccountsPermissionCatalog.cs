using YallaJo.SharedKernel.Application.Authorization;

namespace Accounts.Contracts.Authorization;

public sealed class AccountsPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Accounts";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new(AccountsFeatures.Profile, AppAction.Read,       PermissionGroup.SystemAccess, "View own profile"),
        new(AccountsFeatures.Profile, AppAction.Update,     PermissionGroup.SystemAccess, "Update own profile"),
        new(AccountsFeatures.Profile, AppAction.Delete,     PermissionGroup.SystemAccess, "Delete own profile"),
        new(AccountsFeatures.Profile, AppAction.SoftDelete, PermissionGroup.SystemAccess, "Soft-delete own profile"),

        new(AccountsFeatures.ProviderApplication, AppAction.Read,   PermissionGroup.SystemAccess, "View provider application status"),
        new(AccountsFeatures.ProviderApplication, AppAction.Create, PermissionGroup.SystemAccess, "Create provider application"),
    ];
}
