namespace Booking.Infrastructure.BackgroundServices.Options;

public sealed class BookingCommissionDefaultsOptions
{
    public const string SectionName = "Booking:CommissionDefaults";

    public string Tier { get; set; } = "Free";

    public string Currency { get; set; } = "JOD";

    public decimal FallbackRate { get; set; } = 0.10m;
}
