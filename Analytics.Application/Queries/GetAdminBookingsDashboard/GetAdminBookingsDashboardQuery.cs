using Analytics.Application.Models;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetAdminBookingsDashboard;

public sealed record GetAdminBookingsDashboardQuery(DateTime? From, DateTime? To) : IQuery<AdminBookingsDashboardDto>, ICacheableQuery
{
    public string CacheKey => $"admin:dashboard:bookings:{From:o}:{To:o}";
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => ["admin:dashboard:bookings"];
}
