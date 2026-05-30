namespace Booking.Application.Commands.CreateBulkAvailabilitySlots;

public static class AvailabilitySlotRecurrenceExpander
{
    public static IReadOnlyList<DateOnly> Expand(
        DateOnly startDate,
        DateOnly endDate,
        AvailabilityRecurrence recurrence,
        IReadOnlyList<DayOfWeek>? daysOfWeek)
    {
        return recurrence switch
        {
            AvailabilityRecurrence.Daily => ExpandDaily(startDate, endDate),
            AvailabilityRecurrence.Weekly => ExpandWeekly(startDate, endDate),
            AvailabilityRecurrence.Custom => ExpandCustom(startDate, endDate, daysOfWeek),
            _ => [],
        };
    }

    private static IReadOnlyList<DateOnly> ExpandDaily(DateOnly startDate, DateOnly endDate)
    {
        var dates = new List<DateOnly>();
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            dates.Add(d);
        }

        return dates;
    }

    private static IReadOnlyList<DateOnly> ExpandWeekly(DateOnly startDate, DateOnly endDate)
    {
        var dates = new List<DateOnly>();
        for (var d = startDate; d <= endDate; d = d.AddDays(7))
        {
            dates.Add(d);
        }

        return dates;
    }

    private static IReadOnlyList<DateOnly> ExpandCustom(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<DayOfWeek>? daysOfWeek)
    {
        if (daysOfWeek is null || daysOfWeek.Count == 0)
        {
            return [];
        }

        var daySet = daysOfWeek.Distinct().ToHashSet();
        var dates = new List<DateOnly>();
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            if (daySet.Contains(d.DayOfWeek))
            {
                dates.Add(d);
            }
        }

        return dates;
    }
}
