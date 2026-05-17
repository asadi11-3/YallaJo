using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record ProviderDocumentExpiredDomainEvent(
    Guid DocumentId,
    Guid? TourGuideId,
    Guid? BusinessId) : DomainEventBase;
