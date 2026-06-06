using System.Threading.Channels;
using Analytics.Application.Interfaces;
using Analytics.Application.Models;

namespace Analytics.Infrastructure.Queues;

/// <summary>
/// Bounded in-process channel for the analytics interaction pipeline.
/// FullMode = Wait so the queue NEVER silently drops oldest records: callers
/// either succeed on the fast path (TryWrite) or pay backpressure via
/// EnqueueAsync. This preserves data integrity for downstream aggregation
/// (popularity / trending) while keeping memory bounded to 10k envelopes.
/// </summary>
internal sealed class InteractionIngestQueue : IInteractionIngestQueue
{
    private readonly Channel<InteractionEnvelope> _channel = Channel.CreateBounded<InteractionEnvelope>(
        new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });

    private long _backpressureWaits;
    private long _enqueueFailures;

    public long BackpressureWaitsCount => Interlocked.Read(ref _backpressureWaits);
    public long EnqueueFailuresCount => Interlocked.Read(ref _enqueueFailures);

    public bool TryEnqueue(InteractionEnvelope envelope) => _channel.Writer.TryWrite(envelope);

    public async ValueTask EnqueueAsync(InteractionEnvelope envelope, CancellationToken ct = default)
    {
        // Fast path: try the non-blocking write first to avoid the await cost.
        if (_channel.Writer.TryWrite(envelope)) return;

        // Slow path: queue is full → apply backpressure rather than silently drop.
        Interlocked.Increment(ref _backpressureWaits);
        try
        {
            await _channel.Writer.WriteAsync(envelope, ct).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            Interlocked.Increment(ref _enqueueFailures);
            throw;
        }
    }

    public IAsyncEnumerable<InteractionEnvelope> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
