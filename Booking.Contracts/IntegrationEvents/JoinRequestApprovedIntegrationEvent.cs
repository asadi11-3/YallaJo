using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record JoinRequestApprovedIntegrationEvent(
    Guid JoinRequestId,
    Guid TourBookingId,
    Guid UserId) : IntegrationEventBase;
