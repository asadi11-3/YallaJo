namespace YallaJo.SharedKernel.Application.Authorization;

/// <summary>
/// Cross-cutting permission group constants. Replaces AppRoleGroup.
/// Each module assigns its permissions to one of these groups.
/// Security.Infrastructure uses groups to build role-to-permission mappings.
/// </summary>
public static class PermissionGroup
{
    public const string SystemAccess      = nameof(SystemAccess);
    public const string ContentManagement = nameof(ContentManagement);
    public const string BookingOperations = nameof(BookingOperations);
    public const string FinanceOperations = nameof(FinanceOperations);
    public const string ModerationTools   = nameof(ModerationTools);
    public const string SupportOperations = nameof(SupportOperations);
    public const string AnalyticsAccess   = nameof(AnalyticsAccess);
}
