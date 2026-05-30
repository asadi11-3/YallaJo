using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record ProviderSuspendedDocumentExpiredIntegrationEvent(
    Guid DocumentId,
    Guid? TourGuideId,
    Guid? BusinessId,
    string DocumentType,
    DateTime SuspendedAtUtc) : IntegrationEventBase;
