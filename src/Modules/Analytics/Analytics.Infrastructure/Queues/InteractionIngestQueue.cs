using System.Threading.Channels;
using Analytics.Application.Interfaces;
using Analytics.Application.Models;

namespace Analytics.Infrastructure.Queues;

internal sealed class InteractionIngestQueue : IInteractionIngestQueue
{
    private readonly Channel<InteractionEnvelope> _channel = Channel.CreateBounded<InteractionEnvelope>(
        new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.DropOldest });

    public bool TryEnqueue(InteractionEnvelope envelope) => _channel.Writer.TryWrite(envelope);

    public IAsyncEnumerable<InteractionEnvelope> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
