using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class TourTranslation : BaseEntity
{
    private TourTranslation() { } // EF Core

    public Guid TourId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ShortDescription { get; private set; }
    public string? MeetingPoint { get; private set; }

    public Tour Tour { get; private set; } = default!;

    public static TourTranslation Create(
        Guid tourId,
        Guid languageId,
        string name,
        string? description = null,
        string? shortDescription = null,
        string? meetingPoint = null)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("Tour is required.", nameof(tourId));
        if (languageId == Guid.Empty)
            throw new ArgumentException("Language is required.", nameof(languageId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Translation name is required.", nameof(name));

        return new TourTranslation
        {
            TourId = tourId,
            LanguageId = languageId,
            Name = name.Trim(),
            Description = description?.Trim(),
            ShortDescription = shortDescription?.Trim(),
            MeetingPoint = meetingPoint?.Trim()
        };
    }
}
