using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Queries;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentPlaces.Domain.Repositories;

public interface IPlaceRepository : IRepository<Place, Guid>
{
    Task<bool> HasActiveLinkedBusinessesAsync(Guid placeId, CancellationToken ct = default);

    /// <summary>
    /// Returns places within <paramref name="radiusKm"/> of the given coordinates,
    /// ordered by distance ascending, using a SQL Haversine query pushed to the database.
    /// </summary>
    Task<IReadOnlyList<NearbyPlaceResult>> GetNearbyAsync(
        double lat,
        double lng,
        double radiusKm,
        int pageSize,
        CancellationToken ct = default);
}
