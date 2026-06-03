namespace YallaJo.Web.Areas.Provider.Models.TourSchedules;

public sealed record CreateTourScheduleApiRequest(
    string Pattern,
    List<byte> DaysOfWeek,
    string StartTime,
    string? EndTime,
    bool IsActive);

public sealed record UpdateTourScheduleApiRequest(
    byte DayOfWeek,
    string StartTime,
    string? EndTime,
    bool IsActive);
