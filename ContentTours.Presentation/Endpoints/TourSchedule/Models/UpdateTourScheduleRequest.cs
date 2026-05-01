namespace ContentTours.Presentation.Endpoints.TourSchedule.Models;

public sealed record UpdateTourScheduleRequest(
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    bool IsActive);
