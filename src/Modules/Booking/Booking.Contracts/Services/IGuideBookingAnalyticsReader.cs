namespace Booking.Contracts.Services;

public interface IGuideBookingAnalyticsReader
{
    Task<GuideBookingOverview> GetOverviewAsync(Guid guideUserId, CancellationToken ct = default);
    Task<IReadOnlyList<BookingTrend>> GetBookingTrendsAsync(Guid guideUserId, string granularity, int months, CancellationToken ct = default);
    Task<IReadOnlyList<PopularTour>> GetPopularToursAsync(Guid guideUserId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<PeakDayStat>> GetPeakDaysAsync(Guid guideUserId, CancellationToken ct = default);
}

public sealed record GuideBookingOverview(int TotalBookings, int UpcomingBookings, int CompletedBookings, decimal CancellationRate, decimal AverageRating);
public sealed record BookingTrend(string Period, int BookingCount);
public sealed record PopularTour(Guid TourId, string TourName, int BookingCount);
public sealed record PeakDayStat(string DayOfWeek, int BookingCount);
