using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetProviderAnalytics;

public sealed record GetProviderAnalyticsQuery(Guid ProviderId, DateTime? From, DateTime? To) : IQuery<ProviderAnalyticsDto>, ICacheableQuery
{
    public string CacheKey => $"provider:analytics:{ProviderId}:{From:o}:{To:o}";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(60);
    public IReadOnlyList<string> Tags => [$"provider:analytics:{ProviderId}"];
}
