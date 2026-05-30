using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Domain.Entities;

/// <summary>
/// Per-guide pricing configuration for a specific tour offering.
/// Each guide sets their own prices independent of other guides.
/// </summary>
public sealed class GuidePricingTier : BaseEntity
{
    private GuidePricingTier()
    {
    }

    public Guid GuideTourOfferingId { get; private set; }
    public Guid TourGuideId { get; private set; }
    public Guid TourId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = Money.Zero("JOD");
    public int MinParticipants { get; private set; } = 1;
    public int MaxParticipants { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;

    public static GuidePricingTier Create(
        Guid guideTourOfferingId,
        Guid tourGuideId,
        Guid tourId,
        string name,
        Money price,
        int minParticipants,
        int maxParticipants,
        string? description = null)
    {
        return new GuidePricingTier
        {
            GuideTourOfferingId = guideTourOfferingId,
            TourGuideId = tourGuideId,
            TourId = tourId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Price = price,
            MinParticipants = minParticipants,
            MaxParticipants = maxParticipants,
            IsActive = true
        };
    }

    public void Update(string name, Money price, int minParticipants, int maxParticipants, string? description)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Price = price;
        MinParticipants = minParticipants;
        MaxParticipants = maxParticipants;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
