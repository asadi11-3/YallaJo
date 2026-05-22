namespace ContentTours.Domain.Entities;

public sealed class TourGuideSpecialization
{
    private TourGuideSpecialization()
    {
    }

    public Guid TourGuideId { get; private set; }
    public Guid SpecializationId { get; private set; }

    public TourGuide TourGuide { get; private set; } = default!;

    public static TourGuideSpecialization Create(Guid tourGuideId, Guid specializationId)
    {
        return new TourGuideSpecialization
        {
            TourGuideId = tourGuideId,
            SpecializationId = specializationId
        };
    }
}
