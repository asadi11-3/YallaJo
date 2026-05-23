using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Queries;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentPlaces.Domain.Repositories;

public interface IBusinessRepository : IRepository<Business, Guid>
{
    Task AddPlaceBusinessJunctionAsync(Guid placeId, Guid businessId, CancellationToken ct = default);
    Task RemovePlaceBusinessJunctionAsync(Guid placeId, Guid businessId, CancellationToken ct = default);

    /// <summary>Checks whether a Place with the given Id exists and is not soft-deleted.</summary>
    Task<bool> PlaceExistsAsync(Guid placeId, CancellationToken ct = default);

    /// <summary>
    /// Loads a Business with all navigation collections needed for the detail view:
    /// BusinessTranslations, BusinessHours, ServiceItems (count), Staff (count), Amenities (count).
    /// </summary>
    Task<Business?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all BusinessHours rows for a given business, ordered by DayOfWeek then OpenTime.</summary>
    Task<IReadOnlyList<BusinessHours>> GetBusinessHoursAsync(Guid businessId, CancellationToken ct = default);

    /// <summary>Deletes all existing BusinessHours for the business and inserts the new set atomically.</summary>
    Task ReplaceBusinessHoursAsync(Guid businessId, IEnumerable<BusinessHours> newHours, CancellationToken ct = default);

    /// <summary>Returns nearby businesses using Haversine formula with bounding-box pre-filter.</summary>
    Task<IReadOnlyList<NearbyBusinessResult>> GetNearbyAsync(
        double lat,
        double lng,
        double radiusKm,
        int pageSize,
        CancellationToken ct = default);
}
