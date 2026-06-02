namespace YallaJo.Web.Areas.Accounts.Models.Settings;

/// <summary>
/// Outbound body for PUT /api/v1/accounts/me/marketing-consent.
/// </summary>
public sealed record MarketingConsentRequest(bool EmailDigest, bool PushNotifications, bool ReEngagementCampaigns);
