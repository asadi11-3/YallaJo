using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record JoinRequestRejectedDomainEvent(
    Guid JoinRequestId,
    Guid TourBookingId,
    Guid UserId,
    string Reason) : DomainEventBase;
