namespace YallaJo.Web.Areas.Provider.Models.TourPricing;

public sealed class TourPricingIndexVm
{
    public Guid TourId { get; init; }
    public string TourName { get; init; } = string.Empty;
    public string TourStatusLabel { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;

    public List<TourPricingRowVm> Tiers { get; init; } = [];

    public bool HasTiers => Tiers.Count > 0;
    public bool HasActiveAdultTier => Tiers.Any(t => t.IsActive && t.IsAdult);
}

public sealed class TourPricingRowVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ParticipantType { get; init; } = string.Empty;
    public string ParticipantTypeLabel { get; init; } = string.Empty;
    public bool IsAdult { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int MinParticipants { get; init; }
    public int? MaxParticipants { get; init; }
    public bool IsActive { get; init; }

    public string ParticipantsLabel =>
        MaxParticipants.HasValue ? $"{MinParticipants}–{MaxParticipants}" : $"{MinParticipants}+";
}
