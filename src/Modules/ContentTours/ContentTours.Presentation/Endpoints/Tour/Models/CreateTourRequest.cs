using ContentTours.Domain.Enums;

namespace ContentTours.Presentation.Endpoints.Tour.Models;

public sealed record CreateTourRequest(
    string Name,
    string Slug,
    Difficulty Difficulty,
    int DurationMinutes,
    int MaxGroupSize,
    decimal BasePrice,
    string Currency,
    decimal Latitude,
    decimal Longitude,
    string? Description = null,
    string? ShortDescription = null,
    int? MinAge = null,
    decimal? MeetingPointLatitude = null,
    decimal? MeetingPointLongitude = null,
    Guid? PlaceId = null,
    bool IsChildFriendly = false,
    bool IsAccessible = false,
    int? AgeRestriction = null,
    bool IsInstantBooking = false,
    int CancellationPolicyHours = 24,
    string? MetaTitle = null,
    string? MetaDescription = null);
