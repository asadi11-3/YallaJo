namespace ContentPlaces.Contracts.Places;

public interface IPlaceExistenceService
{
    Task<PlaceExistenceStatus> GetStatusAsync(Guid? placeId, CancellationToken ct = default);
}
