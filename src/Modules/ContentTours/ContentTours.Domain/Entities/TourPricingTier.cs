using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Domain.Entities;

public sealed class TourPricingTier : BaseEntity
{
    private TourPricingTier() { } // EF Core

    public Guid TourId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = default!;

    /// <summary>Derived from Price.Currency — no separate DB column.</summary>
    public string Currency => Price.Currency;

    public int MinParticipants { get; private set; } = 1;
    public int? MaxParticipants { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Typed participant classification. Replaces the fragile magic-string "Adult" check.
    /// Submit gate and adult-tier guards use this field instead of Name comparison.
    /// </summary>
    public ParticipantType ParticipantType { get; private set; } = ParticipantType.Other;

    public Tour Tour { get; private set; } = default!;

    private readonly List<TourPricingTierTranslation> _translations = [];
    public IReadOnlyCollection<TourPricingTierTranslation> Translations => _translations.AsReadOnly();

    /// <summary>True when this is the required Adult tier (for submit-gate and deletion guard).</summary>
    public bool IsAdult => ParticipantType == ParticipantType.Adult;

    /// <summary>Factory method. Currency is embedded in <paramref name="price"/>.</summary>
    public static TourPricingTier Create(
        Guid tourId,
        string name,
        string? description,
        Money price,
        ParticipantType participantType,
        int minParticipants,
        int? maxParticipants)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (minParticipants < 1)
            throw new ArgumentOutOfRangeException(nameof(minParticipants), "MinParticipants must be at least 1.");
        if (maxParticipants is { } m && m <= minParticipants)
            throw new ArgumentException("MaxParticipants must be greater than MinParticipants.", nameof(maxParticipants));

        return new TourPricingTier
        {
            TourId          = tourId,
            Name            = name.Trim(),
            Description     = description?.Trim(),
            Price           = price,
            ParticipantType = participantType,
            MinParticipants = minParticipants,
            MaxParticipants = maxParticipants,
            IsActive        = true,
        };
    }

    /// <summary>Updates all mutable fields. Caller must check Adult-tier guard before calling with IsActive=false or renaming away from Adult.</summary>
    public void Update(
        string name,
        string? description,
        Money price,
        ParticipantType participantType,
        int minParticipants,
        int? maxParticipants,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (minParticipants < 1)
            throw new ArgumentOutOfRangeException(nameof(minParticipants));
        if (maxParticipants is { } m && m <= minParticipants)
            throw new ArgumentException("MaxParticipants must be greater than MinParticipants.", nameof(maxParticipants));

        Name            = name.Trim();
        Description     = description?.Trim();
        Price           = price;
        ParticipantType = participantType;
        MinParticipants = minParticipants;
        MaxParticipants = maxParticipants;
        IsActive        = isActive;
    }

    /// <summary>Deactivates the tier (reversible). Caller must check Adult-tier guard first.</summary>
    public void Deactivate() => IsActive = false;
}
