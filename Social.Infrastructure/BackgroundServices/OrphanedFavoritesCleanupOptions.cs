namespace Social.Infrastructure.BackgroundServices;

/// <summary>Configuration for the weekly orphaned-favorites cleanup job.</summary>
public sealed class OrphanedFavoritesCleanupOptions
{
    public const string SectionName = "Social:BackgroundServices:OrphanedFavoritesCleanup";

    /// <summary>Whether the service is enabled (default true).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Day of week to run (default Saturday).</summary>
    public DayOfWeek TargetDayOfWeek { get; set; } = DayOfWeek.Saturday;

    /// <summary>UTC time of day to run (default 03:00).</summary>
    public TimeSpan TargetTimeUtc { get; set; } = new TimeSpan(3, 0, 0);

    /// <summary>Maximum favorites to process per batch (default 500).</summary>
    public int BatchSize { get; set; } = 500;
}
