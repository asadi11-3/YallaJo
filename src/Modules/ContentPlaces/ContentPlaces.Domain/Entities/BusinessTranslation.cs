using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class BusinessTranslation : BaseEntity
{
    private BusinessTranslation() { } // EF Core

    public Guid BusinessId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Address { get; private set; }

    public Business Business { get; private set; } = default!;

    public static BusinessTranslation Create(
        Guid businessId,
        Guid languageId,
        string name,
        string? description = null,
        string? address = null)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("Business is required.", nameof(businessId));
        if (languageId == Guid.Empty)
            throw new ArgumentException("Language is required.", nameof(languageId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Translation name is required.", nameof(name));

        return new BusinessTranslation
        {
            BusinessId = businessId,
            LanguageId = languageId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Address = address?.Trim()
        };
    }
}
