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
        new(AnalyticsFeatures.Recommendation, AppAction.Read, PermissionGroup.AnalyticsAccess, "Read recommendations"),
        new(AnalyticsFeatures.Preference, AppAction.Read, PermissionGroup.AnalyticsAccess, "Read own preferences"),
        new(AnalyticsFeatures.Preference, AppAction.Update, PermissionGroup.AnalyticsAccess, "Update own preferences"),
        new(AnalyticsFeatures.Batch, AppAction.Read, PermissionGroup.AnalyticsAccess, "View suggestion batches"),
        new(AnalyticsFeatures.Batch, AppAction.Refresh, PermissionGroup.AnalyticsAccess, "Refresh suggestion batches"),
        new(AnalyticsFeatures.AdminDashboard, AppAction.Read, PermissionGroup.AnalyticsAccess, "View admin analytics dashboard"),
        new(AnalyticsFeatures.AdminDashboard, AppAction.Export, PermissionGroup.AnalyticsAccess, "Export admin analytics dashboard"),
        new(AnalyticsFeatures.AdminDashboard, AppAction.Refresh, PermissionGroup.AnalyticsAccess, "Refresh admin analytics dashboard"),
        new(AnalyticsFeatures.ProviderDashboard, AppAction.Read, PermissionGroup.AnalyticsAccess, "View provider analytics dashboard"),
        new(AnalyticsFeatures.ProviderDashboard, AppAction.Export, PermissionGroup.AnalyticsAccess, "Export provider analytics dashboard"),
        new(AnalyticsFeatures.AuditLog, AppAction.Read, PermissionGroup.SystemAccess, "Read audit logs"),
        new(AnalyticsFeatures.AuditLog, AppAction.Export, PermissionGroup.SystemAccess, "Export audit logs"),
        new(AnalyticsFeatures.AuditLog, AppAction.Redact, PermissionGroup.SystemAccess, "Redact audit logs"),
        new(AnalyticsFeatures.BoostPackage, AppAction.Read, PermissionGroup.AnalyticsAccess, "View boost packages"),
        new(AnalyticsFeatures.BoostPackage, AppAction.Create, PermissionGroup.AnalyticsAccess, "Create boost packages"),
        new(AnalyticsFeatures.BoostPackage, AppAction.Delete, PermissionGroup.AnalyticsAccess, "Delete boost packages"),
        new(AnalyticsFeatures.EditorialPin, AppAction.Read, PermissionGroup.AnalyticsAccess, "View editorial pins"),
        new(AnalyticsFeatures.EditorialPin, AppAction.Create, PermissionGroup.AnalyticsAccess, "Create editorial pins"),
        new(AnalyticsFeatures.SeasonalityRule, AppAction.Read, PermissionGroup.AnalyticsAccess, "View seasonality rules"),
        new(AnalyticsFeatures.SeasonalityRule, AppAction.Create, PermissionGroup.AnalyticsAccess, "Create seasonality rules"),
        new(AnalyticsFeatures.SeasonalityRule, AppAction.Delete, PermissionGroup.AnalyticsAccess, "Delete seasonality rules"),
        new(AnalyticsFeatures.HolidayCalendar, AppAction.Read, PermissionGroup.AnalyticsAccess, "View holiday calendars"),
        new(AnalyticsFeatures.HolidayCalendar, AppAction.Create, PermissionGroup.AnalyticsAccess, "Create holiday calendars"),
        new(AnalyticsFeatures.Photogenic, AppAction.Update, PermissionGroup.AnalyticsAccess, "Update photogenic flags"),
        new(AnalyticsFeatures.Experiment, AppAction.Read, PermissionGroup.AnalyticsAccess, "View experiments"),
        new(AnalyticsFeatures.Experiment, AppAction.Create, PermissionGroup.AnalyticsAccess, "Create experiments"),
        new(AnalyticsFeatures.Experiment, AppAction.Delete, PermissionGroup.AnalyticsAccess, "Delete experiments"),
        new(AnalyticsFeatures.GuideDashboard, AppAction.Read, PermissionGroup.AnalyticsAccess, "View guide analytics dashboard"),
        new(AnalyticsFeatures.GuideDashboard, AppAction.Export, PermissionGroup.AnalyticsAccess, "Export guide analytics dashboard"),
    ];
}
