using ContentTours.Domain.Enums;

namespace ContentTours.Application.Queries.TourPricingTier.Common;

public sealed record TourPricingTierDto(
    Guid Id,
    Guid TourId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    /// <summary>Typed participant classification (Adult, Child, Senior, …). Consumers use this to identify the Adult tier.</summary>
    ParticipantType ParticipantType,
    int MinParticipants,
    int? MaxParticipants,
    bool IsActive,
    DateTime CreatedAt);
