using ContentTours.Domain.Enums;

namespace ContentTours.Domain.Entities;

public sealed class TourChildFacility
{
    private TourChildFacility()
    {
    }

    public TourChildFacility(Guid tourId, ChildFacility facility)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        TourId = tourId;
        Facility = facility;
    }

    public Guid TourId { get; private set; }

    public ChildFacility Facility { get; private set; }

    public Tour Tour { get; private set; } = default!;
}
