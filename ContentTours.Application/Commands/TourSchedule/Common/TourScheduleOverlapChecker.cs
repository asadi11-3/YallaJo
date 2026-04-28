using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourSchedule.Common;

/// <summary>
/// Detects time-interval overlaps across TourSchedule rows grouped by DayOfWeek.
/// Intervals are half-open [StartTime, EndTime). EndTime==null = open-ended (treated as TimeOnly.MaxValue).
/// Two rows on the same DayOfWeek overlap iff b.StartTime &lt; (a.EndTime ?? TimeOnly.MaxValue).
/// </summary>
internal static class TourScheduleOverlapChecker
{
    public static Error? Check(
        IEnumerable<Domain.Entities.TourSchedule> existing,
        IEnumerable<(byte DayOfWeek, TimeOnly Start, TimeOnly? End)> candidates)
    {
        var byDay = new Dictionary<byte, List<(TimeOnly Start, TimeOnly? End)>>();

        foreach (var s in existing)
        {
            if (!byDay.TryGetValue(s.DayOfWeek, out var list))
                byDay[s.DayOfWeek] = list = [];
            list.Add((s.StartTime, s.EndTime));
        }

        foreach (var (dow, start, end) in candidates)
        {
            if (!byDay.TryGetValue(dow, out var list))
                byDay[dow] = list = [];
            list.Add((start, end));
        }

        foreach (var (dow, rows) in byDay)
        {
            var sorted = rows.OrderBy(r => r.Start).ToList();
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var a = sorted[i];
                var b = sorted[i + 1];
                var endA = a.End ?? TimeOnly.MaxValue;
                if (b.Start < endA)
                    return new Error("TourSchedule.OverlapDetected",
                        $"Overlap on DayOfWeek={dow}: [{a.Start}–{a.End?.ToString() ?? "open"}] " +
                        $"conflicts with [{b.Start}–{b.End?.ToString() ?? "open"}].");
            }
        }

        return null;
    }
}
