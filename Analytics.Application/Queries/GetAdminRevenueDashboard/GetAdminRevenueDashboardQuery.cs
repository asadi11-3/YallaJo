using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetAdminRevenueDashboard;

public sealed record GetAdminRevenueDashboardQuery(DateTime? From, DateTime? To) : IQuery<AdminRevenueDashboardDto>, ICacheableQuery
{
    public string CacheKey => $"admin:dashboard:revenue:{From:o}:{To:o}";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => ["admin:dashboard:revenue"];
}
