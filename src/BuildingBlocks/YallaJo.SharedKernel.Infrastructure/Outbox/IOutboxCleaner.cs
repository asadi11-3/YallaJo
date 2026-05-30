namespace YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Per-module cleanup + dead-letter management abstraction.
/// Each module registers its own implementation:
///   services.AddScoped&lt;IOutboxCleaner, OutboxCleaner&lt;{Module}DbContext&gt;&gt;();
///
/// <see cref="BackgroundJobs.OutboxCleanupBackgroundService"/> resolves all registered
/// implementations and iterates over them each cleanup tick.
/// </summary>
public interface IOutboxCleaner
{
    /// <summary>Stable module identifier used for logging and routing.</summary>
    string ModuleName { get; }

    /// <summary>
    /// Deletes successfully processed rows older than <paramref name="cutoffUtc"/>.
    /// NEVER deletes dead-lettered rows (RetryCount &gt;= MaxRetryCount).
    /// Returns the total count of rows deleted across all batch iterations.
    /// </summary>
    Task<int> DeleteProcessedBeforeAsync(DateTime cutoffUtc, int batchSize, CancellationToken ct);

    /// <summary>
    /// Returns the count of dead-lettered messages (RetryCount &gt;= MaxRetryCount) in this module.
    /// </summary>
    Task<int> CountDeadLetteredAsync(CancellationToken ct);

    /// <summary>
    /// Returns a summary list of dead-lettered messages for ops visibility.
    /// </summary>
    Task<IReadOnlyList<OutboxDeadLetterDto>> ListDeadLetteredAsync(int limit, CancellationToken ct);

    /// <summary>
    /// Replays a dead-lettered message by cloning it with a fresh Id and zeroed RetryCount.
    /// The original row is preserved for audit. Returns <c>true</c> if found + replayed.
    /// </summary>
    Task<bool> ReplayDeadLetterAsync(Guid messageId, CancellationToken ct);
}
