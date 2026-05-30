namespace Booking.Infrastructure.BackgroundServices.Options;

public sealed class DocumentExpiryCheckOptions
{
    public const string SectionName = "Booking:BackgroundServices:DocumentExpiryCheck";

    public bool Enabled { get; set; } = true;

    public TimeOnly TargetUtcTime { get; set; } = new(1, 0);

    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(5);

    public int BatchSize { get; set; } = 1000;

    public int ExpiringSoonWindowDays { get; set; } = 30;
}
