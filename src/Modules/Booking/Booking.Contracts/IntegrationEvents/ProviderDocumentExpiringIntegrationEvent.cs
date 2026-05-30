using YallaJo.SharedKernel.Domain.Event;

namespace Booking.Contracts.IntegrationEvents;

public sealed record ProviderDocumentExpiringIntegrationEvent(
    Guid DocumentId,
    Guid? TourGuideId,
    Guid? BusinessId,
    DateTime ExpiresAt) : IntegrationEventBase;
