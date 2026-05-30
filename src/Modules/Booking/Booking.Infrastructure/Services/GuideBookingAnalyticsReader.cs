using Booking.Contracts.Services;
using Booking.Domain.Enums;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Services;

internal sealed class GuideBookingAnalyticsReader(BookingDbContext context) : IGuideBookingAnalyticsReader
{
    public async Task<GuideBookingOverview> GetOverviewAsync(Guid guideUserId, CancellationToken ct = default)
    {
        var guide = await context.TourGuides
            .AsNoTracking()
            .Where(g => g.UserId == guideUserId)
            .Select(g => new { g.Id, g.AverageRating })
            .FirstOrDefaultAsync(ct);

        if (guide is null)
        {
            return new GuideBookingOverview(0, 0, 0, 0m, 0m);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var total = await context.TourBookings.AsNoTracking().CountAsync(b => b.GuideId == guide.Id, ct);
        var completed = await context.TourBookings.AsNoTracking().CountAsync(b => b.GuideId == guide.Id && b.Status == BookingStatus.Completed, ct);
        var cancelled = await context.TourBookings.AsNoTracking().CountAsync(b => b.GuideId == guide.Id && b.Status == BookingStatus.Cancelled, ct);
        var upcoming = await (
            from booking in context.TourBookings.AsNoTracking()
            join slot in context.AvailabilitySlots.AsNoTracking() on booking.AvailabilitySlotId equals slot.Id
            where booking.GuideId == guide.Id
                && booking.Status == BookingStatus.Confirmed
                && slot.Date >= today
            select booking.Id)
            .CountAsync(ct);

        var cancellationRate = total > 0 ? Math.Round((decimal)cancelled / total, 4) : 0m;
        return new GuideBookingOverview(total, upcoming, completed, cancellationRate, guide.AverageRating);
    }

    public async Task<IReadOnlyList<BookingTrend>> GetBookingTrendsAsync(Guid guideUserId, string granularity, int months, CancellationToken ct = default)
    {
        var guideId = await GetGuideIdAsync(guideUserId, ct);
        if (guideId is null)
        {
            return [];
        }

        months = Math.Clamp(months, 1, 24);
        var start = DateTime.UtcNow.AddMonths(-months);
        var monthly = !string.Equals(granularity, "weekly", StringComparison.OrdinalIgnoreCase);

        var rows = await context.TourBookings
            .AsNoTracking()
            .Where(b => b.GuideId == guideId.Value && b.CreatedAt >= start)
            .Select(b => new { b.CreatedAt })
            .ToListAsync(ct);

        return rows
            .GroupBy(row => monthly
                ? row.CreatedAt.ToString("yyyy-MM")
                : $"{row.CreatedAt.Year}-W{System.Globalization.ISOWeek.GetWeekOfYear(row.CreatedAt):00}")
            .OrderBy(g => g.Key)
            .Select(g => new BookingTrend(g.Key, g.Count()))
            .ToList();
    }

    public async Task<IReadOnlyList<PopularTour>> GetPopularToursAsync(Guid guideUserId, int limit, CancellationToken ct = default)
    {
        var guideId = await GetGuideIdAsync(guideUserId, ct);
        if (guideId is null)
        {
            return [];
        }

        limit = Math.Clamp(limit, 1, 50);
        var rows = await context.TourBookings
            .AsNoTracking()
            .Where(b => b.GuideId == guideId.Value)
            .GroupBy(b => b.TourId)
            .Select(g => new { TourId = g.Key, BookingCount = g.Count() })
            .OrderByDescending(x => x.BookingCount)
            .Take(limit)
            .ToListAsync(ct);

        return rows
            .Select(row => new PopularTour(row.TourId, $"Tour {row.TourId.ToString("N")[..8]}", row.BookingCount))
            .ToList();
    }

    public async Task<IReadOnlyList<PeakDayStat>> GetPeakDaysAsync(Guid guideUserId, CancellationToken ct = default)
    {
        var guideId = await GetGuideIdAsync(guideUserId, ct);
        if (guideId is null)
        {
            return [];
        }

        var rows = await (
            from booking in context.TourBookings.AsNoTracking()
            join slot in context.AvailabilitySlots.AsNoTracking() on booking.AvailabilitySlotId equals slot.Id
            where booking.GuideId == guideId.Value
            select slot.Date)
            .ToListAsync(ct);

        return rows
            .GroupBy(date => date.DayOfWeek.ToString())
            .OrderByDescending(g => g.Count())
            .Select(g => new PeakDayStat(g.Key, g.Count()))
            .ToList();
    }

    private async Task<Guid?> GetGuideIdAsync(Guid guideUserId, CancellationToken ct)
        => await context.TourGuides
            .AsNoTracking()
            .Where(g => g.UserId == guideUserId)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(ct);
}
