using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record SlotLockCreatedDomainEvent(
    Guid SlotLockId,
    Guid AvailabilitySlotId,
    Guid UserId,
    DateTime ExpiresAt) : DomainEventBase;
