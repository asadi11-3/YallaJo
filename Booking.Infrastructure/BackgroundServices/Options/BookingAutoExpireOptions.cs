namespace Booking.Infrastructure.BackgroundServices.Options;

public sealed class BookingAutoExpireOptions
{
    public const string SectionName = "Booking:BackgroundServices:BookingAutoExpire";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(1);

    public int BatchSize { get; set; } = 500;

    public int PaymentWindowMinutes { get; set; } = 10;
}
