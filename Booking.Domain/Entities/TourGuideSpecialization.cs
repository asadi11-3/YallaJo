namespace Booking.Domain.Entities;

public sealed class TourGuideSpecialization
{
    private TourGuideSpecialization() { } // EF Core

    public Guid TourGuideId { get; private set; }
    public Guid SpecializationId { get; private set; }

    public TourGuide TourGuide { get; private set; } = default!;
}
