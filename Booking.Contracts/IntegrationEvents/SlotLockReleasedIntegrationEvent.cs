using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record SlotLockReleasedIntegrationEvent(
    Guid SlotLockId,
    Guid AvailabilitySlotId) : IntegrationEventBase;
