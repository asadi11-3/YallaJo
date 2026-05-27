using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetProviderDashboard;

public sealed record GetProviderDashboardQuery(Guid ProviderId) : IQuery<ProviderDashboardDto>, ICacheableQuery
{
    public string CacheKey => $"provider:dashboard:{ProviderId}";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(60);
    public IReadOnlyList<string> Tags => [$"provider:dashboard:{ProviderId}"];
}
