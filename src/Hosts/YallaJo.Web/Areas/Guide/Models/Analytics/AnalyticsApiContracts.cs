namespace YallaJo.Web.Areas.Guide.Models.Analytics;

public sealed record GuideBookingOverviewResponse(
    int TotalBookings,
    int UpcomingBookings,
    int CompletedBookings,
    decimal CancellationRate,
    decimal AverageRating);

public sealed record BookingTrendResponse(string Period, int BookingCount);

public sealed record PopularTourResponse(Guid TourId, string TourName, int BookingCount);

public sealed record PeakDayStatResponse(string DayOfWeek, int BookingCount);
