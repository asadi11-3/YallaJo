namespace ContentTours.Domain.Entities;

public sealed class TourTourGuide
{
    private TourTourGuide()
    {
    } // EF Core

    public Guid TourId { get; private set; }
    public Guid TourGuideId { get; private set; }
    public bool IsPrimary { get; private set; }

    public Tour Tour { get; private set; } = default!;


    public static TourTourGuide Create(Guid tourId, Guid tourGuideId, bool isPrimary)
    {
        return new TourTourGuide
        {
            TourId = tourId,
            TourGuideId = tourGuideId,
            IsPrimary = isPrimary
        };
    }

    public void SetAsNonPrimary()
    {
        IsPrimary = false;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }
}
