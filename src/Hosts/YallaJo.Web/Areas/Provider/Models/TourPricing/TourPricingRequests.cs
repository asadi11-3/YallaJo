namespace YallaJo.Web.Areas.Provider.Models.TourPricing;

public sealed record CreateTourPricingTierApiRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string ParticipantType,
    int MinParticipants,
    int? MaxParticipants);

public sealed record UpdateTourPricingTierApiRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string ParticipantType,
    int MinParticipants,
    int? MaxParticipants,
    bool IsActive);
