using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetAdminDashboardOverview;

public sealed record GetAdminDashboardOverviewQuery() : IQuery<AdminDashboardOverviewDto>, ICacheableQuery
{
    public string CacheKey => "admin:dashboard:overview";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => ["admin:dashboard:overview"];
}
