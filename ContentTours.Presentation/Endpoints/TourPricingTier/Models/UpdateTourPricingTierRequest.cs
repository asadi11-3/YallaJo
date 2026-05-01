using ContentTours.Domain.Enums;

namespace ContentTours.Presentation.Endpoints.TourPricingTier.Models;

public sealed record UpdateTourPricingTierRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    ParticipantType ParticipantType,
    int MinParticipants,
    int? MaxParticipants,
    bool IsActive);
