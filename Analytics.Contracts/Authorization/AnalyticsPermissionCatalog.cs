using YallaJo.SharedKernel.Application.Authorization;

namespace Analytics.Contracts.Authorization;

public sealed class AnalyticsPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Analytics";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new(AnalyticsFeatures.Interaction, AppAction.Create, PermissionGroup.AnalyticsAccess, "Record interactions"),
        new(AnalyticsFeatures.Interaction, AppAction.Read, PermissionGroup.AnalyticsAccess, "Read interactions"),
        new(AnalyticsFeatures.PopularityScore, AppAction.Read, PermissionGroup.AnalyticsAccess, "View popularity scores"),
        new(AnalyticsFeatures.PopularityScore, AppAction.Refresh, PermissionGroup.AnalyticsAccess, "Refresh popularity scores"),
        new(AnalyticsFeatures.PopularityScore, AppAction.Trigger, PermissionGroup.AnalyticsAccess, "Trigger popularity calculation"),
        new(AnalyticsFeatures.Trending, AppAction.Read, PermissionGroup.AnalyticsAccess, "View trending entities"),
        new(AnalyticsFeatures.AdminDashboard, AppAction.Read, PermissionGroup.AnalyticsAccess, "View admin analytics dashboard"),
        new(AnalyticsFeatures.AdminDashboard, AppAction.Export, PermissionGroup.AnalyticsAccess, "Export admin analytics dashboard"),
        new(AnalyticsFeatures.AdminDashboard, AppAction.Refresh, PermissionGroup.AnalyticsAccess, "Refresh admin analytics dashboard"),
        new(AnalyticsFeatures.ProviderDashboard, AppAction.Read, PermissionGroup.AnalyticsAccess, "View provider analytics dashboard"),
        new(AnalyticsFeatures.ProviderDashboard, AppAction.Export, PermissionGroup.AnalyticsAccess, "Export provider analytics dashboard"),
        new(AnalyticsFeatures.AuditLog, AppAction.Read, PermissionGroup.SystemAccess, "Read audit logs"),
        new(AnalyticsFeatures.AuditLog, AppAction.Export, PermissionGroup.SystemAccess, "Export audit logs"),
        new(AnalyticsFeatures.AuditLog, AppAction.Redact, PermissionGroup.SystemAccess, "Redact audit logs"),
    ];
}
