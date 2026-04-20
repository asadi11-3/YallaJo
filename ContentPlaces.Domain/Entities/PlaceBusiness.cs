namespace ContentPlaces.Domain.Entities;

public sealed class PlaceBusiness
{
    private PlaceBusiness()
    {
    } // EF Core

    public Guid PlaceId { get; private set; }
    public Guid BusinessId { get; private set; }

    public static PlaceBusiness Create(Guid placeId, Guid businessId)
    {
        if (placeId == Guid.Empty) throw new ArgumentException("PlaceId cannot be empty.", nameof(placeId));
        if (businessId == Guid.Empty) throw new ArgumentException("BusinessId cannot be empty.", nameof(businessId));

        return new PlaceBusiness
        {
            PlaceId = placeId,
            BusinessId = businessId
        };
    }
}
