using Analytics.Application.Interfaces;
using Analytics.Application.Models;
using Analytics.Domain.Enums;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Services;

internal sealed class AnalyticsDashboardReader(AnalyticsDbContext db) : IAnalyticsDashboardReader
{
    public async Task<AdminDashboardOverviewDto> GetAdminOverviewAsync(CancellationToken ct)
    {
        var revenue = await db.PaymentSnapshots.AsNoTracking().Where(x => x.Type == "Payment" && x.Status == "Completed").SumAsync(x => x.Amount, ct);
        var bookings = await db.BookingSnapshots.AsNoTracking().CountAsync(ct);
        var users = await db.UserInteractions.AsNoTracking().Where(x => x.UserId != null).Select(x => x.UserId).Distinct().CountAsync(ct);
        var alerts = await db.PopularityScores.AsNoTracking().CountAsync(x => x.IsStale, ct);
        return new AdminDashboardOverviewDto(revenue, bookings, users, alerts);
    }

    public async Task<AdminRevenueDashboardDto> GetAdminRevenueAsync(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = FilterPayments(from, to).Where(x => x.Type == "Payment" && x.Status == "Completed");
        var grouped = await q.GroupBy(x => x.CompletedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.Amount) })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);
        var series = grouped.Select(x => new RevenueTimePointDto(x.Date, x.Revenue)).ToList();
        return new AdminRevenueDashboardDto(series, series.Sum(x => x.Revenue));
    }

    public async Task<AdminBookingsDashboardDto> GetAdminBookingsAsync(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = FilterBookings(from, to);
        var rows = await q.GroupBy(x => x.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        var total = rows.Sum(x => x.Count);
        return new AdminBookingsDashboardDto(total, rows.Where(x => x.Status == "Completed").Sum(x => x.Count), rows.Where(x => x.Status == "Cancelled").Sum(x => x.Count));
    }

    public async Task<AdminUsersDashboardDto> GetAdminUsersAsync(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var all = db.UserInteractions.AsNoTracking().Where(x => x.UserId != null);
        var filtered = FilterInteractions(from, to).Where(x => x.UserId != null);
        return new AdminUsersDashboardDto(await all.Select(x => x.UserId).Distinct().CountAsync(ct), await filtered.Select(x => x.UserId).Distinct().CountAsync(ct));
    }

    public async Task<ProviderDashboardDto> GetProviderDashboardAsync(Guid providerId, CancellationToken ct)
    {
        var revenue = await db.PaymentSnapshots.AsNoTracking().Where(x => x.ProviderId == providerId && x.Type == "Payment" && x.Status == "Completed").SumAsync(x => x.Amount, ct);
        var bookings = await db.BookingSnapshots.AsNoTracking().CountAsync(x => x.ProviderId == providerId, ct);
        var tourIds = await db.BookingSnapshots.AsNoTracking().Where(x => x.ProviderId == providerId).Select(x => x.TourId).Distinct().ToListAsync(ct);
        var rating = await db.PopularityScores.AsNoTracking().Where(x => x.EntityType == EntityType.Tour && tourIds.Contains(x.EntityId) && x.ReviewCountSnapshot > 0 && x.AverageRatingSnapshot != null).AverageAsync(x => (decimal?)x.AverageRatingSnapshot, ct) ?? 0m;
        return new ProviderDashboardDto(providerId, revenue, bookings, rating);
    }

    public async Task<ProviderAnalyticsDto> GetProviderAnalyticsAsync(Guid providerId, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var q = FilterPayments(from, to).Where(x => x.ProviderId == providerId && x.Type == "Payment" && x.Status == "Completed");
        var grouped = await q.GroupBy(x => x.CompletedAt.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.Amount) })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);
        var series = grouped.Select(x => new RevenueTimePointDto(x.Date, x.Revenue)).ToList();
        return new ProviderAnalyticsDto(providerId, series);
    }

    public async Task<CursorPageDto<ProviderTourListItemDto>> GetProviderToursAsync(Guid providerId, long? afterId, int pageSize, CancellationToken ct)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var tourIds = db.BookingSnapshots.AsNoTracking().Where(x => x.ProviderId == providerId).Select(x => x.TourId).Distinct();
        var query = db.PopularityScores.AsNoTracking().Where(x => x.EntityType == EntityType.Tour && tourIds.Contains(x.EntityId));
        if (afterId is not null) query = query.Where(x => x.Score < afterId.Value);
        var rows = await query.OrderByDescending(x => x.Score).Take(size + 1).ToListAsync(ct);
        var next = rows.Count > size ? (long?)rows[^1].Score : null;
        return new CursorPageDto<ProviderTourListItemDto>(rows.Take(size).Select(x => new ProviderTourListItemDto(x.EntityId, x.EntityId.ToString(), x.Score)).ToList(), next);
    }

    private IQueryable<Analytics.Domain.Entities.PaymentSnapshot> FilterPayments(DateTime? from, DateTime? to)
    {
        var q = db.PaymentSnapshots.AsNoTracking().AsQueryable();
        if (from is not null) q = q.Where(x => x.CompletedAt >= from.Value);
        if (to is not null) q = q.Where(x => x.CompletedAt <= to.Value);
        return q;
    }

    private IQueryable<Analytics.Domain.Entities.BookingSnapshot> FilterBookings(DateTime? from, DateTime? to)
    {
        var q = db.BookingSnapshots.AsNoTracking().AsQueryable();
        if (from is not null) q = q.Where(x => x.CreatedAtSnapshot >= from.Value);
        if (to is not null) q = q.Where(x => x.CreatedAtSnapshot <= to.Value);
        return q;
    }

    private IQueryable<Analytics.Domain.Entities.UserInteraction> FilterInteractions(DateTime? from, DateTime? to)
    {
        var q = db.UserInteractions.AsNoTracking().AsQueryable();
        if (from is not null) q = q.Where(x => x.OccurredAt >= from.Value);
        if (to is not null) q = q.Where(x => x.OccurredAt <= to.Value);
        return q;
    }
}
