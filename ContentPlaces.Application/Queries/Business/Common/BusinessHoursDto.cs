namespace ContentPlaces.Application.Queries.Business.Common;

/// <summary>Represents a single time-slot entry for a day of the week.</summary>
public sealed record BusinessHoursDto(
    Guid Id,
    string DayOfWeek,
    string? OpenTime,
    string? CloseTime,
    bool IsClosed);
