using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record SlotLockReleasedDomainEvent(
    Guid SlotLockId,
    Guid AvailabilitySlotId) : DomainEventBase;
