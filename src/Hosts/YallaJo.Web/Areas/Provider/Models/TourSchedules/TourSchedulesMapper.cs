using System.Globalization;

namespace YallaJo.Web.Areas.Provider.Models.TourSchedules;

public static class TourSchedulesMapper
{
    private static readonly string[] DayNames =
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    // ── List ────────────────────────────────────────────────────────────────────────

    public static TourSchedulesIndexVm ToIndexVm(
        Guid tourId, string tourName, string tourStatusLabel,
        IReadOnlyList<TourScheduleResponse> schedules) => new()
    {
        TourId          = tourId,
        TourName        = tourName,
        TourStatusLabel = tourStatusLabel,
        Schedules       = schedules
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime, StringComparer.Ordinal)
            .Select(ToRowVm)
            .ToList(),
    };

    private static TourScheduleRowVm ToRowVm(TourScheduleResponse s) => new()
    {
        Id        = s.Id,
        DayOfWeek = s.DayOfWeek,
        DayLabel  = DayLabel(s.DayOfWeek),
        StartTime = FormatTime(s.StartTime),
        EndTime   = string.IsNullOrEmpty(s.EndTime) ? null : FormatTime(s.EndTime),
        IsActive  = s.IsActive,
    };

    // ── Form ─────────────────────────────────────────────────────────────────────────

    public static TourScheduleFormVm ToCreateVm(Guid tourId, string tourName) => new()
    {
        TourId    = tourId,
        TourName  = tourName,
        DayOfWeek = 1,        // Monday — a sensible default
        IsActive  = true,
    };

    public static TourScheduleFormVm ToEditVm(Guid tourId, string tourName, TourScheduleResponse s) => new()
    {
        TourId     = tourId,
        ScheduleId = s.Id,
        TourName   = tourName,
        DayOfWeek  = s.DayOfWeek,
        StartTime  = ParseTime(s.StartTime),
        EndTime    = string.IsNullOrEmpty(s.EndTime) ? null : ParseTime(s.EndTime),
        IsActive   = s.IsActive,
    };

    // ── Form → API request ───────────────────────────────────────────────────────────

    public static CreateTourScheduleApiRequest ToCreateRequest(TourScheduleFormVm vm) => new(
        Pattern:     "Weekly",
        DaysOfWeek:  [vm.DayOfWeek],
        StartTime:   ToApiTime(vm.StartTime),
        EndTime:     vm.EndTime.HasValue ? ToApiTime(vm.EndTime) : null,
        IsActive:    true);

    public static UpdateTourScheduleApiRequest ToUpdateRequest(TourScheduleFormVm vm) => new(
        DayOfWeek:  vm.DayOfWeek,
        StartTime:  ToApiTime(vm.StartTime),
        EndTime:    vm.EndTime.HasValue ? ToApiTime(vm.EndTime) : null,
        IsActive:   vm.IsActive);

    // ── Helpers ──────────────────────────────────────────────────────────────────────

    public static string DayLabel(byte dayOfWeek) =>
        dayOfWeek < DayNames.Length ? DayNames[dayOfWeek] : $"Day {dayOfWeek}";

    // API TimeOnly serializes as "HH:mm:ss"; send that format.
    private static string ToApiTime(TimeOnly? time) =>
        (time ?? default).ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    // Parse "HH:mm:ss"/"HH:mm" from the API into a TimeOnly for the edit form.
    private static TimeOnly? ParseTime(string value) =>
        TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var t) ? t : null;

    // Format an API time string ("HH:mm:ss") for display as "HH:mm".
    private static string FormatTime(string value) =>
        TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var t)
            ? t.ToString("HH:mm", CultureInfo.InvariantCulture)
            : value;
}
