namespace ContentTours.Application.Queries.TourSchedule.Common;

public sealed record TourScheduleDto(
    Guid Id,
    Guid TourId,
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    bool IsActive,
    DateTime CreatedAt);
