namespace Booking.Domain.Entities;

public sealed class TourGuideLanguage
{
    private TourGuideLanguage() { } // EF Core

    public Guid TourGuideId { get; private set; }
    public Guid LanguageId { get; private set; }
    public byte ProficiencyLevel { get; private set; }

    public TourGuide TourGuide { get; private set; } = default!;
}
