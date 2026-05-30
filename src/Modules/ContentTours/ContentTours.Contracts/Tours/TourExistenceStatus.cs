namespace ContentTours.Contracts.Tours;

public enum TourExistenceStatus
{
    /// <summary>Default uninitialized value; should not be returned by the service.</summary>
    Unknown = 0,

    /// <summary>The TourId references a live, non-deleted Tour.</summary>
    Active = 1,

    /// <summary>The TourId does not match any Tour row.</summary>
    NotFound = 2,

    /// <summary>The TourId matches a Tour that has been soft-deleted.</summary>
    Deleted = 3,
}
