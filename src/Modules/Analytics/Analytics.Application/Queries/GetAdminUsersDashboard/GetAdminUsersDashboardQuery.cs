using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetAdminUsersDashboard;

public sealed record GetAdminUsersDashboardQuery(DateTime? From, DateTime? To) : IQuery<AdminUsersDashboardDto>, ICacheableQuery
{
    public string CacheKey => $"admin:dashboard:users:{From:o}:{To:o}";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => ["admin:dashboard:users"];
}
