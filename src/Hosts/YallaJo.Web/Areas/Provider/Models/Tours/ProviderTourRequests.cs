namespace YallaJo.Web.Areas.Provider.Models.Tours;

public sealed record CreateTourApiRequest(
    string Name,
    string Slug,
    string Difficulty,
    int DurationMinutes,
    int MaxGroupSize,
    decimal BasePrice,
    string Currency,
    decimal Latitude,
    decimal Longitude,
    string? Description,
    string? ShortDescription,
    Guid? PlaceId,
    bool IsChildFriendly,
    bool IsAccessible,
    int CancellationPolicyHours,
    decimal? MeetingPointLatitude,
    decimal? MeetingPointLongitude);

public sealed record UpdateTourApiRequest(
    byte[] RowVersion,
    string Name,
    string Slug,
    string Difficulty,
    int DurationMinutes,
    int MaxGroupSize,
    decimal BasePrice,
    string Currency,
    decimal Latitude,
    decimal Longitude,
    string? Description,
    string? ShortDescription,
    Guid? PlaceId,
    bool IsChildFriendly,
    bool IsAccessible,
    int CancellationPolicyHours,
    decimal? MeetingPointLatitude,
    decimal? MeetingPointLongitude);

public sealed record TourRowVersionApiRequest(byte[] RowVersion);
