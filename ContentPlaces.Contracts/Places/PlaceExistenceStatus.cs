namespace ContentPlaces.Contracts.Places;

public enum PlaceExistenceStatus
{
    /// <summary>No PlaceId was supplied; nothing to check.</summary>
    NotChecked = 0,

    /// <summary>The PlaceId references a live, non-deleted Place.</summary>
    Active = 1,

    /// <summary>The PlaceId does not match any Place row.</summary>
    NotFound = 2,

    /// <summary>The PlaceId matches a Place that has been soft-deleted.</summary>
    Deleted = 3,
}
