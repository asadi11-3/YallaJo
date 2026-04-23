using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

/// <summary>
/// Raised by Tour whenever a change might affect the active-tour count for a Place.
/// The domain event handler re-queries the live count and publishes a
/// <c>PlaceTourCountUpdatedIntegrationEvent</c> so ContentPlaces can update its
/// denormalized <see cref="ContentPlaces.Domain.Entities.Place.TourCount"/>.
///
/// Carrying the PlaceId (nullable) lets the handler know which place to re-count.
/// Null = this tour was never linked to a place — handler is a no-op.
/// </summary>
public sealed record TourPlaceCountChangedDomainEvent(
    Guid TourId,
    Guid? PlaceId) : DomainEventBase;
