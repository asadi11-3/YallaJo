using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record AvailabilitySlotCapacityChangedIntegrationEvent(
    Guid SlotId,
    Guid? TourId,
    int OldCapacity,
    int NewCapacity) : IntegrationEventBase;
