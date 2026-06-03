namespace YallaJo.Web.Areas.Provider.Models.TourSchedules;

public sealed class TourSchedulesIndexVm
{
    public Guid TourId { get; init; }
    public string TourName { get; init; } = string.Empty;
    public string TourStatusLabel { get; init; } = string.Empty;

    public List<TourScheduleRowVm> Schedules { get; init; } = [];

    public bool HasSchedules => Schedules.Count > 0;
    public bool HasActiveSchedule => Schedules.Any(s => s.IsActive);
}

public sealed class TourScheduleRowVm
{
    public Guid Id { get; init; }
    public byte DayOfWeek { get; init; }
    public string DayLabel { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string? EndTime { get; init; }
    public bool IsActive { get; init; }

    public string TimeRangeLabel =>
        string.IsNullOrEmpty(EndTime) ? StartTime : $"{StartTime} – {EndTime}";
}
