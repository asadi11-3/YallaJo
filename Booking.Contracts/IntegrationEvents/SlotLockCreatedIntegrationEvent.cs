using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record SlotLockCreatedIntegrationEvent(
    Guid SlotLockId,
    Guid AvailabilitySlotId,
    Guid UserId,
    DateTime ExpiresAt) : IntegrationEventBase;
