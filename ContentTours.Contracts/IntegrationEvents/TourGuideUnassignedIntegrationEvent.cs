using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts.IntegrationEvents;

/// <summary>
/// Raised when a Tour Guide is unassigned from a Tour.
/// Consumers: Messaging (notify guide), Booking (block future bookings that depended on this guide).
/// </summary>
public sealed record TourGuideUnassignedIntegrationEvent(
    Guid TourId,
    Guid TourGuideUserId,
    Guid UnassignedByUserId
) : IntegrationEventBase;
