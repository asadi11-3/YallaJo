namespace YallaJo.Web.Areas.Business.Models.Hours;

public static class HoursMapper
{
    private static readonly string[] DayNames =
    [
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday",
    ];

    /// <summary>
    /// Builds a fixed 7-day form (Sunday=0 .. Saturday=6) seeding the first existing
    /// entry per day. Backend stores DayOfWeek as a string name; map it back to an index.
    /// </summary>
    public static List<DayHoursFormVm> ToDays(IReadOnlyList<BusinessHoursItemResponse> existing)
    {
        var byDay = new Dictionary<int, BusinessHoursItemResponse>();
        foreach (var entry in existing)
        {
            var index = DayIndex(entry.DayOfWeek);
            if (index >= 0 && !byDay.ContainsKey(index))
                byDay[index] = entry;
        }

        var days = new List<DayHoursFormVm>(7);
        for (var i = 0; i < 7; i++)
        {
            byDay.TryGetValue(i, out var match);
            days.Add(new DayHoursFormVm
            {
                DayOfWeek = i,
                DayName = DayNames[i],
                IsClosed = match?.IsClosed ?? false,
                OpenTime = Trim(match?.OpenTime),
                CloseTime = Trim(match?.CloseTime),
            });
        }
        return days;
    }

    private static int DayIndex(string? dayName)
    {
        if (string.IsNullOrWhiteSpace(dayName)) return -1;
        for (var i = 0; i < DayNames.Length; i++)
        {
            if (string.Equals(DayNames[i], dayName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    // Trim seconds for display in <input type="time"> which expects HH:mm.
    private static string? Trim(string? time)
    {
        if (string.IsNullOrWhiteSpace(time)) return null;
        var parts = time.Split(':');
        return parts.Length >= 2 ? $"{parts[0]}:{parts[1]}" : time;
    }
}
