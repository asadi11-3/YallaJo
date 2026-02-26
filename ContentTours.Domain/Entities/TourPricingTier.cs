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
    public string Currency { get; private set; } = string.Empty;
    public int MinParticipants { get; private set; } = 1;
    public int? MaxParticipants { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Tour Tour { get; private set; } = default!;
}
