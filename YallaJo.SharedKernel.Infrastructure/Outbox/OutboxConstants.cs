namespace YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Shared constants for the outbox subsystem. Single source of truth.
/// </summary>
public static class OutboxConstants
{
    /// <summary>
    /// Maximum retry attempts before a message is dead-lettered.
    /// Must match <see cref="BackgroundJobs.OutboxProcessor{TContext}.MaxRetryCount"/>.
    /// </summary>
    public const int MaxRetryCount = 10;
}
