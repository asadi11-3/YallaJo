using Analytics.Application.Models;

namespace Analytics.Application.Interfaces;

public interface IInteractionIngestQueue
{
    bool TryEnqueue(InteractionEnvelope envelope);
    IAsyncEnumerable<InteractionEnvelope> ReadAllAsync(CancellationToken ct);
}
