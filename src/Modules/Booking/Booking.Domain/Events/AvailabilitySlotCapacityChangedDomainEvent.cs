using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record AvailabilitySlotCapacityChangedDomainEvent(
    Guid SlotId,
    int OldCapacity,
    int NewCapacity) : DomainEventBase;
