using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class PlaceTranslation : BaseEntity
{
    private PlaceTranslation() { } // EF Core

    public Guid PlaceId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Address { get; private set; }

    public Place Place { get; private set; } = default!;

    public static PlaceTranslation Create(
        Guid placeId,
        Guid languageId,
        string name,
        string? description = null,
        string? address = null)
    {
        if (placeId == Guid.Empty)
            throw new ArgumentException("Place is required.", nameof(placeId));
        if (languageId == Guid.Empty)
            throw new ArgumentException("Language is required.", nameof(languageId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Translation name is required.", nameof(name));

        return new PlaceTranslation
        {
            PlaceId = placeId,
            LanguageId = languageId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Address = address?.Trim()
        };
    }
}
