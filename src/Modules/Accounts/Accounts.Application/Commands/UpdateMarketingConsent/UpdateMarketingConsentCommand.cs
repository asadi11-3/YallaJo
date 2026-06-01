using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.UpdateMarketingConsent;

public sealed record UpdateMarketingConsentCommand(
    bool EmailDigest,
    bool PushNotifications,
    bool ReEngagementCampaigns) : ICommand<MarketingConsentResult>;


