using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class TourPricingTierTranslation : BaseEntity
{
    private TourPricingTierTranslation() { }

    public Guid TourPricingTierId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public TourPricingTier TourPricingTier { get; private set; } = default!;

    public static TourPricingTierTranslation Create(
        Guid tierId,
        string languageCode,
        string name,
        string? description = null)
    {
        if (tierId == Guid.Empty)
            throw new ArgumentException("Tour pricing tier is required.", nameof(tierId));
        if (string.IsNullOrWhiteSpace(languageCode))
            throw new ArgumentException("Language code is required.", nameof(languageCode));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Translation name is required.", nameof(name));

        return new TourPricingTierTranslation
        {
            TourPricingTierId = tierId,
            LanguageCode = languageCode.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Description = description?.Trim()
        };
    }

    public void Update(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Translation name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }
}
