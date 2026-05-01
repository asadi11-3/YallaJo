using ContentTours.Domain.Enums;

namespace ContentTours.Presentation.Endpoints.TourPricingTier.Models;

public sealed record CreateTourPricingTierRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    ParticipantType ParticipantType,
    int MinParticipants = 1,
    int? MaxParticipants = null);
