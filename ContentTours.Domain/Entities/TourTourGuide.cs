namespace ContentTours.Domain.Entities;

public sealed class TourTourGuide
{
    private TourTourGuide() { } // EF Core

    public Guid TourId { get; private set; }
    public Guid TourGuideId { get; private set; }
    public bool IsPrimary { get; private set; }
}
