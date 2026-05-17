using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record ProviderDocumentExpiringDomainEvent(
    Guid DocumentId,
    Guid? TourGuideId,
    Guid? BusinessId,
    DateTime ExpiresAt) : DomainEventBase;
