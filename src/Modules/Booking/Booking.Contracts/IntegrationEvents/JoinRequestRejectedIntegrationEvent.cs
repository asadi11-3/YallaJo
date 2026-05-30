using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record JoinRequestRejectedIntegrationEvent(
    Guid JoinRequestId,
    Guid TourBookingId,
    Guid UserId,
    string Reason) : IntegrationEventBase;
