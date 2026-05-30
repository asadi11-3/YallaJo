namespace Booking.Infrastructure.BackgroundServices.Options;

public sealed class SlotLockCleanupOptions
{
    public const string SectionName = "Booking:BackgroundServices:SlotLockCleanup";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);

    public int BatchSize { get; set; } = 500;
}
