using YallaJo.Web.Areas.Auth.Models.Sessions;

namespace YallaJo.Web.Areas.Accounts.Models.Settings;

/// <summary>
/// Composite view model for the account settings page.
/// </summary>
public sealed class SettingsVm
{
    public IReadOnlyList<NotificationRowVm> NotificationRows { get; init; } = [];
    public MarketingConsentVm Marketing { get; init; } = new();
    public string? PhoneNumber { get; init; }
    public IReadOnlyList<SessionItemVm> Sessions { get; init; } = [];
}

/// <summary>
/// One notification type row with the toggle state for each surfaced channel.
/// </summary>
public sealed class NotificationRowVm
{
    public string Type { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool Email { get; init; } = true;
    public bool Push { get; init; } = true;
    public bool InApp { get; init; } = true;
}

public sealed class MarketingConsentVm
{
    public bool EmailDigest { get; init; }
    public bool PushNotifications { get; init; }
    public bool ReEngagementCampaigns { get; init; }
}
