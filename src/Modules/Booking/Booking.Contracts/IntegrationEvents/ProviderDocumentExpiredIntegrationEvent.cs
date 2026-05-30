using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record ProviderDocumentExpiredIntegrationEvent(
    Guid DocumentId,
    Guid? TourGuideId,
    Guid? BusinessId) : IntegrationEventBase;
