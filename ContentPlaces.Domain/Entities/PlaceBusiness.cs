namespace ContentPlaces.Domain.Entities;

public sealed class PlaceBusiness
{
    private PlaceBusiness() { } // EF Core

    public Guid PlaceId { get; private set; }
    public Guid BusinessId { get; private set; }
}
