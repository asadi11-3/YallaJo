using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Repositories;

/// <summary>
/// Module-local outbox writer for Accounts integration events.
/// Application handlers call this to enqueue integration events in the same transaction
/// as their state change. The shared <c>OutboxProcessor&lt;AccountsDbContext&gt;</c> dispatches.
/// </summary>
public interface IAccountsOutboxWriter
{
    /// <summary>Serializes and enqueues <paramref name="integrationEvent"/> for outbound delivery.</summary>
    Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
