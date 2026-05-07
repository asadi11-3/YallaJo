namespace ContentTours.Contracts.Authorization;

public static class ContentToursFeatures
{
    public const string Tour = nameof(Tour);
    public const string Package = nameof(Package);

    // ── Sub-resources of a Tour (managed via dedicated endpoints) ────────────
    public const string TourGuide         = nameof(TourGuide);
    public const string TourPricingTier   = nameof(TourPricingTier);
    public const string TourSchedule      = nameof(TourSchedule);
    public const string TourWaypoint      = nameof(TourWaypoint);
    public const string TourChildrenInfo  = nameof(TourChildrenInfo);
}
