using Analytics.Application.Models;

namespace Analytics.Application.Interfaces;

/// <summary>
/// Bounded in-process channel for buffering analytics interactions before the
/// drain service flushes them to the UserInteractions table.
/// </summary>
/// <remarks>
/// The queue is configured to apply backpressure (no silent drops). Callers
/// that can wait should prefer <see cref="EnqueueAsync"/>; the fast-path
/// <see cref="TryEnqueue"/> returns false when the queue is full so the caller
/// can decide whether to log/skip or fall back to <see cref="EnqueueAsync"/>.
/// </remarks>
public interface IInteractionIngestQueue
{
    /// <summary>Fast non-blocking enqueue. Returns false when the queue is full.</summary>
    bool TryEnqueue(InteractionEnvelope envelope);

    /// <summary>Backpressure-aware enqueue: returns immediately when there is room,
    /// otherwise awaits until capacity is freed by the drain service. Tracks how
    /// often backpressure waits occurred so operators can observe lossy conditions.</summary>
    ValueTask EnqueueAsync(InteractionEnvelope envelope, CancellationToken ct = default);

    IAsyncEnumerable<InteractionEnvelope> ReadAllAsync(CancellationToken ct);

    /// <summary>Number of times <see cref="EnqueueAsync"/> had to wait for capacity (lifetime, since process start).</summary>
    long BackpressureWaitsCount { get; }

    /// <summary>Number of <see cref="EnqueueAsync"/> calls that failed (e.g. channel closed) (lifetime).</summary>
    long EnqueueFailuresCount { get; }
}
