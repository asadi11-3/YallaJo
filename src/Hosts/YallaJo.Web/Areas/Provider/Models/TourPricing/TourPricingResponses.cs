namespace YallaJo.Web.Areas.Provider.Models.TourPricing;

public sealed class TourPricingTierResponse
{
    public Guid Id { get; init; }
    public Guid TourId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string ParticipantType { get; init; } = string.Empty;
    public int MinParticipants { get; init; }
    public int? MaxParticipants { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class CreateTourPricingTierResponse
{
    public Guid Id { get; init; }
}
