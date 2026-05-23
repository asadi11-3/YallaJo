using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Domain.Events;

public sealed record ProviderSuspendedDocumentExpiredDomainEvent(
    Guid DocumentId,
    Guid? TourGuideId,
    Guid? BusinessId,
    DocumentType DocumentType,
    DateTime SuspendedAtUtc) : DomainEventBase;
