using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Booking.Infrastructure.Diagnostics;

public static class BookingDiagnostics
{
    public const string SourceName = "YallaJo.Booking";
    public const string MeterName = "YallaJo.Booking";

    public static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> BgServiceTicks =
        Meter.CreateCounter<long>(
            "bg_service_ticks_total",
            description: "Total background-service ticks");

    public static readonly Counter<long> BgServiceFailures =
        Meter.CreateCounter<long>(
            "bg_service_failures_total",
            description: "Total background-service ticks that ended in error");

    public static readonly Counter<long> BgServiceItemsProcessed =
        Meter.CreateCounter<long>(
            "bg_service_items_processed_total",
            description: "Total items processed across all ticks of a background service");
}
