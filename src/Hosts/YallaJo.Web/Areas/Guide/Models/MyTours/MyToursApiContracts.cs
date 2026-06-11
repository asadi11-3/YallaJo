namespace YallaJo.Web.Areas.Guide.Models.MyTours;

// Response DTOs (mirror ContentTours.Application.Queries.GuideTourOffering.*)
public sealed record GuideOfferingDto(
    Guid Id,
    Guid TourId,
    Guid TourGuideId,
    string Status,
    bool OffersPrivateTour,
    decimal? PrivateTourPriceMultiplier,
    decimal? PrivateTourFlatPrice,
    bool IsProposer,
    DateTime? AssignedAt,
    DateTime CreatedAt);

public sealed record GuideOfferingDetailDto(
    Guid Id,
    Guid TourId,
    Guid TourGuideId,
    string Status,
    bool OffersPrivateTour,
    decimal? PrivateTourPriceMultiplier,
    decimal? PrivateTourFlatPrice,
    bool IsProposer,
    DateTime? AssignedAt,
    DateTime CreatedAt,
    IReadOnlyList<GuideScheduleDto> Schedules,
    IReadOnlyList<GuidePricingTierDto> PricingTiers);

public sealed record GuideScheduleDto(
    Guid Id,
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    bool IsActive,
    DateTime CreatedAt);

public sealed record GuidePricingTierDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int MinParticipants,
    int MaxParticipants,
    bool IsActive,
    DateTime CreatedAt);

// Request DTOs (mirror GuideOfferingEndpoints.cs request records).
// Schedule times are sent as strings ("HH:mm") and parsed to TimeOnly server-side.
public sealed record CreateScheduleRequest(byte DayOfWeek, string StartTime, string? EndTime);

public sealed record CreatePricingTierRequest(
    string Name,
    decimal Price,
    string Currency,
    int MinParticipants,
    int MaxParticipants,
    string? Description);

public sealed record EnablePrivateTourRequest(decimal? Multiplier, decimal? FlatPrice);
