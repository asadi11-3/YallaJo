using Accounts.Application.Caching;
using Accounts.Application.Commands.UpdateMarketingConsent;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.GetMarketingConsent;

public sealed record GetMarketingConsentQuery(Guid UserId) : IQuery<MarketingConsentResult>, ICacheableQuery
{
    public string CacheKey => $"accounts:profile:{UserId}:marketing-consent";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => [AccountsCacheKeys.UserProfileTag(UserId)];
}
