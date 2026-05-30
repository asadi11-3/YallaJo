namespace YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Configuration for <see cref="BackgroundJobs.OutboxCleanupBackgroundService"/>.
/// Bind from appsettings section "OutboxCleanup".
/// </summary>
public sealed class OutboxCleanupOptions
{
    /// <summary>Master toggle. Set false to disable the cleanup service entirely.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How long to keep successfully processed rows before deletion.
    /// Default: 30 days. Dead-lettered rows are NEVER deleted regardless of this setting.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>How often the cleanup loop runs. Default: 1 hour.</summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Rows deleted per SQL statement. Smaller batches = shorter transactions + fewer locks.
    /// Default: 1000.
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}
