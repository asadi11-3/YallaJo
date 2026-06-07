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

    /// <summary>§3.10 Devices tab — registered push-notification device tokens.</summary>
    public IReadOnlyList<DeviceTokenRowVm> Devices { get; init; } = [];

    /// <summary>
    /// §3.10 Linked accounts tab — supported external providers with their link state.
    /// State is derived from existing claims/local data only (no GET-list endpoint).
    /// </summary>
    public IReadOnlyList<LinkedAccountVm> LinkedAccounts { get; init; } = [];
}

/// <summary>
/// One external sign-in provider row. Linking happens through the sign-in OAuth flow
/// (Auth area); unlinking posts to <c>DELETE /auth/external-providers/{providerId}</c>.
/// <para>
/// <c>GET /security/me</c> returns only JWT claims (no linked-provider list) and the rule
/// forbids creating a GET-list endpoint, so <see cref="IsLinked"/> stays unknown unless a
/// future claim exposes it — the UI therefore offers the action without asserting state.
/// </para>
/// </summary>
public sealed record LinkedAccountVm(string Provider, string DisplayName, bool IsLinked);

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
