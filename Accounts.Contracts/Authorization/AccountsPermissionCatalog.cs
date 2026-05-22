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

        new(AccountsFeatures.ProviderApplication, AppAction.Read,       PermissionGroup.SystemAccess, "View provider application status"),
        new(AccountsFeatures.ProviderApplication, AppAction.Create,     PermissionGroup.SystemAccess, "Create provider application"),
        new(AccountsFeatures.ProviderApplication, AppAction.Register,   PermissionGroup.SystemAccess, "Register as a provider (initial draft)"),
        new(AccountsFeatures.ProviderApplication, AppAction.Submit,     PermissionGroup.SystemAccess, "Submit provider application for review"),

        new(AccountsFeatures.AdminProviderQueue, AppAction.Read,        PermissionGroup.ModerationTools, "View provider application queue"),
        new(AccountsFeatures.AdminProviderQueue, AppAction.Approve,     PermissionGroup.ModerationTools, "Approve provider application"),
        new(AccountsFeatures.AdminProviderQueue, AppAction.Reject,      PermissionGroup.ModerationTools, "Reject provider application"),
        new(AccountsFeatures.AdminProviderQueue, AppAction.RequestDocs, PermissionGroup.ModerationTools, "Request additional documents from applicant"),
        new(AccountsFeatures.AdminProviderQueue, AppAction.Suspend,     PermissionGroup.ModerationTools, "Suspend an approved provider"),
        new(AccountsFeatures.AdminProviderQueue, AppAction.Reinstate,   PermissionGroup.ModerationTools, "Reinstate a suspended provider"),
    ];
}
