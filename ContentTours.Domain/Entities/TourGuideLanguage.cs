namespace ContentTours.Domain.Entities;

public sealed class TourGuideLanguage
{
    private TourGuideLanguage()
    {
    }

    public Guid TourGuideId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Proficiency { get; private set; } = string.Empty;

    public TourGuide TourGuide { get; private set; } = default!;

    public static TourGuideLanguage Create(Guid tourGuideId, Guid languageId, string proficiency)
    {
        return new TourGuideLanguage
        {
            TourGuideId = tourGuideId,
            LanguageId = languageId,
            Proficiency = proficiency
        };
    }
}
