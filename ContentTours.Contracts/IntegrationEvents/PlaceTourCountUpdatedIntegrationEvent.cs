using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts.IntegrationEvents;

/// <summary>
/// Published by ContentTours whenever the active (Published, not deleted) tour count
/// changes for a given Place. ContentPlaces consumes this event to keep its denormalised
/// <c>Place.TourCount</c> column in sync.
///
/// ContentTours owns the authoritative count — it re-queries on every relevant Tour state
/// change and sends the fresh total. This makes the event idempotent and safe to retry.
/// </summary>
public sealed record PlaceTourCountUpdatedIntegrationEvent(
    Guid PlaceId,
    int ActiveTourCount) : IntegrationEventBase;
