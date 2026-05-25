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

        // Agency roster permissions (agency managing their guides)
        new(AccountsFeatures.AgencyRoster, AppAction.Read,    PermissionGroup.ContentManagement, "View agency guide roster"),
        new(AccountsFeatures.AgencyRoster, AppAction.Create,  PermissionGroup.ContentManagement, "Invite a guide to agency"),
        new(AccountsFeatures.AgencyRoster, AppAction.Delete,  PermissionGroup.ContentManagement, "Remove a guide from agency"),
        new(AccountsFeatures.AgencyRoster, AppAction.Approve, PermissionGroup.ContentManagement, "Approve a guide application to agency"),
        new(AccountsFeatures.AgencyRoster, AppAction.Reject,  PermissionGroup.ContentManagement, "Reject a guide application to agency"),

        // Guide-side agency permissions (guide managing their agency relationship)
        new(AccountsFeatures.GuideAgency, AppAction.Read,   PermissionGroup.ContentManagement, "View agency invitations and applications"),
        new(AccountsFeatures.GuideAgency, AppAction.Create, PermissionGroup.ContentManagement, "Apply to join an agency"),
        new(AccountsFeatures.GuideAgency, AppAction.Update, PermissionGroup.ContentManagement, "Accept or decline agency invitation"),
        new(AccountsFeatures.GuideAgency, AppAction.Delete, PermissionGroup.ContentManagement, "Leave current agency"),

        // Provider dashboard
        new(AccountsFeatures.ProviderDashboard, AppAction.Read, PermissionGroup.SystemAccess, "View provider dashboard overview"),
    ];
}
