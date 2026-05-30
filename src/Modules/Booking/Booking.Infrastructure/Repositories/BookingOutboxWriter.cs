using Booking.Domain.Repositories;
using Booking.Infrastructure.Persistence;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Booking.Infrastructure.Repositories;

/// <summary>
/// Booking module's outbox writer — appends integration events to the OutboxMessages table
/// in the same DbContext as the aggregate change, ensuring atomicity with the SaveChanges commit.
/// </summary>
internal sealed class BookingOutboxWriter(BookingDbContext context) : IBookingOutboxWriter
{
    public Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var message = OutboxMessage.Create(integrationEvent);
        context.OutboxMessages.Add(message);
        return Task.CompletedTask;
    }
}
