using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record AvailabilitySlotCapacityChangedDomainEvent(
    Guid SlotId,
    Guid? TourId,
    int OldCapacity,
    int NewCapacity) : DomainEventBase;
