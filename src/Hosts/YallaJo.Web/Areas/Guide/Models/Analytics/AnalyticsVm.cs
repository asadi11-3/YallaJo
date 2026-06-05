namespace YallaJo.Web.Areas.Guide.Models.Analytics;

public sealed class AnalyticsVm
{
    public int TotalBookings { get; init; }
    public int UpcomingBookings { get; init; }
    public int CompletedBookings { get; init; }
    public decimal CancellationRate { get; init; }
    public decimal AverageRating { get; init; }

    public IReadOnlyList<BookingTrendRowVm> BookingTrends { get; init; } = [];
    public IReadOnlyList<PopularTourRowVm> PopularTours { get; init; } = [];
    public IReadOnlyList<PeakDayRowVm> PeakDays { get; init; } = [];

    public bool HasBookingTrends => BookingTrends.Count > 0;
    public bool HasPopularTours => PopularTours.Count > 0;
    public bool HasPeakDays => PeakDays.Count > 0;

    public int MaxTrendCount => BookingTrends.Count > 0 ? BookingTrends.Max(t => t.BookingCount) : 0;
    public int MaxPeakDayCount => PeakDays.Count > 0 ? PeakDays.Max(d => d.BookingCount) : 0;
}

public sealed record BookingTrendRowVm(string Period, int BookingCount);

public sealed record PopularTourRowVm(Guid TourId, string TourName, int BookingCount);

public sealed record PeakDayRowVm(string DayOfWeek, int BookingCount);
