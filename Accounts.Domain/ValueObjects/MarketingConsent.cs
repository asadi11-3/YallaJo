namespace Accounts.Domain.ValueObjects;

public sealed record MarketingConsent(
    bool EmailDigest,
    bool PushNotifications,
    bool ReEngagementCampaigns,
    DateTime? LastUpdatedUtc);
