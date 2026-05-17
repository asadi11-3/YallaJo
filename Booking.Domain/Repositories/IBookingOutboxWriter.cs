using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Repositories;

/// <summary>
/// Module-local outbox writer for Booking integration events.
/// Application handlers call this to enqueue integration events in the same transaction
/// as their state change. The shared <c>OutboxProcessor&lt;BookingDbContext&gt;</c> dispatches.
/// </summary>
public interface IBookingOutboxWriter
{
    /// <summary>Serializes and enqueues <paramref name="integrationEvent"/> for outbound delivery.</summary>
    Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
