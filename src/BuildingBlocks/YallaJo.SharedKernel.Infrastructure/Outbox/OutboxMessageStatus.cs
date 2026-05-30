namespace YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Explicit status for an outbox message.
/// Replaces implicit status inference from <c>ProcessedOnUtc</c> + <c>RetryCount</c>
/// with a first-class queryable column — enables filtered indexes and fast ops queries.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>Not yet picked up by the processor.</summary>
    Pending = 0,

    /// <summary>Currently locked and being dispatched by a processor instance.</summary>
    Processing = 1,

    /// <summary>All handlers succeeded; message will not be retried.</summary>
    Processed = 2,

    /// <summary>At least one handler failed; RetryCount &lt; MaxRetryCount — will retry.</summary>
    Failed = 3,

    /// <summary>RetryCount &gt;= MaxRetryCount; requires manual investigation/replay.</summary>
    Dead = 4,
}
