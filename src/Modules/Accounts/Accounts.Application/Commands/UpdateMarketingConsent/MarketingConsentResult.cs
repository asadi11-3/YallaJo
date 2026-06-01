using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accounts.Application.Commands.UpdateMarketingConsent
{
    public sealed record MarketingConsentResult(
     bool EmailDigest,
     bool PushNotifications,
     bool ReEngagementCampaigns,
     DateTime? LastUpdatedUtc);
}
