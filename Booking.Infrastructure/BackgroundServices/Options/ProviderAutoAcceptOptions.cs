namespace Booking.Infrastructure.BackgroundServices.Options;

public sealed class ProviderAutoAcceptOptions
{
    public const string SectionName = "Booking:BackgroundServices:ProviderAutoAccept";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(2);

    public int BatchSize { get; set; } = 500;

    public double ProviderConfirmationHours { get; set; } = 24d;

    public TimeSpan ConfirmationWindow => TimeSpan.FromHours(ProviderConfirmationHours);
}
