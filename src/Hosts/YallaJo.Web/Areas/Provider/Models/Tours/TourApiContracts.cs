namespace YallaJo.Web.Areas.Provider.Models.Tours;

// ---- Create ----
public sealed record CreateTourRequest(
    string Name,
    string Slug,
    string Difficulty,
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

public sealed class CreateTourResultResponse
{
    public Guid TourId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

// ---- Schedules ----
public sealed record CreateTourScheduleRequest(
    string Pattern,
    List<byte>? DaysOfWeek,
    List<DateOnly>? CustomDates,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsActive = true);

public sealed class TourScheduleResponse
{
    public Guid Id { get; init; }
    public byte DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public bool IsActive { get; init; }
}

// ---- Pricing tiers ----
public sealed record CreateTourPricingTierRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string ParticipantType,
    int MinParticipants = 1,
    int? MaxParticipants = null);

public sealed class TourPricingTierResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? ParticipantType { get; init; }
    public int MinParticipants { get; init; }
    public int? MaxParticipants { get; init; }
    public bool IsActive { get; init; }
}

// ---- Waypoints ----
public sealed record AddTourWaypointRequest(
    string Name,
    string? Description,
    double Latitude,
    double Longitude,
    string WaypointType,
    int? DurationMinutes);

public sealed class TourWaypointResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public int? DurationMinutes { get; init; }
    public string? WaypointType { get; init; }
}

// ---- Children info ----
public sealed record UpdateChildrenInfoRequest(
    bool AllowsChildren,
    int? MinChildAge,
    int? MaxChildAge,
    string? ChildFacilities);

public sealed class ChildrenInfoResponse
{
    public bool AllowsChildren { get; init; }
    public int? MinChildAge { get; init; }
    public int? MaxChildAge { get; init; }
    public IReadOnlyList<string> ChildFacilities { get; init; } = [];
}
