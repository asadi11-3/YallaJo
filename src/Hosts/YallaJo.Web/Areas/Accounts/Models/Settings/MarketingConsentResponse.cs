namespace YallaJo.Web.Areas.Accounts.Models.Settings;

/// <summary>
/// Inbound marketing-consent state from GET /api/v1/accounts/me/marketing-consent.
/// </summary>
public sealed class MarketingConsentResponse
{
    public bool EmailDigest { get; init; }
    public bool PushNotifications { get; init; }
    public bool ReEngagementCampaigns { get; init; }
    public DateTime? LastUpdatedUtc { get; init; }
}
