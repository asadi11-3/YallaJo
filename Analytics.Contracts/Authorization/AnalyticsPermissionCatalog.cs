using YallaJo.SharedKernel.Application.Authorization;

namespace Analytics.Contracts.Authorization;

public sealed class AnalyticsPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Analytics";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new(AnalyticsFeatures.UserInteraction, AppAction.Record, PermissionGroup.AnalyticsAccess, "Record user interactions"),
        new(AnalyticsFeatures.UserInteraction, AppAction.ReadAny, PermissionGroup.AnalyticsAccess, "Read all user interactions"),
        new(AnalyticsFeatures.PopularityScore, AppAction.Read, PermissionGroup.AnalyticsAccess, "View popularity scores"),
        new(AnalyticsFeatures.PopularityScore, AppAction.Refresh, PermissionGroup.AnalyticsAccess, "Trigger popularity recalculation"),
        new(AnalyticsFeatures.RecommendationCache, AppAction.Read, PermissionGroup.AnalyticsAccess, "View recommendation cache entries"),
        new(AnalyticsFeatures.RecommendationCache, AppAction.Refresh, PermissionGroup.AnalyticsAccess, "Regenerate recommendations"),
        new(AnalyticsFeatures.UserPreference, AppAction.ReadOwn, PermissionGroup.AnalyticsAccess, "Read own preferences"),
        new(AnalyticsFeatures.UserPreference, AppAction.UpdateSelf, PermissionGroup.AnalyticsAccess, "Update own preferences"),
        new(AnalyticsFeatures.AuditLog, AppAction.ReadAny, PermissionGroup.SystemAccess, "Read audit logs"),
        new(AnalyticsFeatures.AuditLog, AppAction.Export, PermissionGroup.SystemAccess, "Export audit logs"),
        new(AnalyticsFeatures.AuditLog, AppAction.Redact, PermissionGroup.SystemAccess, "Redact audit log entries"),
        new(AnalyticsFeatures.AnalyticsAdmin, AppAction.Read, PermissionGroup.SystemAccess, "View analytics admin dashboards"),
        new(AnalyticsFeatures.AnalyticsAdmin, AppAction.Export, PermissionGroup.SystemAccess, "Export analytics bulk data"),
        new(AnalyticsFeatures.AnalyticsAdmin, AppAction.Trigger, PermissionGroup.SystemAccess, "Trigger analytics background jobs"),
    ];
}
