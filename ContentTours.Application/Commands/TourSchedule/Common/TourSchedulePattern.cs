namespace ContentTours.Application.Commands.TourSchedule.Common;

/// <summary>
/// Recurrence pattern descriptor used by <c>CreateTourSchedule</c> to expand a single
/// request into 1..N <see cref="Domain.Entities.TourSchedule"/> rows.
///
/// Per PDF Task-2A B2 (Recurrence Pattern Specification):
///   - <see cref="Once"/>   — single date, single emit. Uses CustomDates[0] if supplied, else ValidFrom.
///   - <see cref="Daily"/>  — emit per day across [ValidFrom, ValidTo].
///   - <see cref="Weekly"/> — emit per day across [ValidFrom, ValidTo] when DayOfWeek is in <c>DaysOfWeek</c>.
///   - <see cref="Custom"/> — emit one candidate per supplied <c>CustomDates</c> (must lie in [today, today+90]).
///
/// Application-layer concept only: the <see cref="Domain.Entities.TourSchedule"/>
/// entity itself stores discrete weekly rows, not the pattern that produced them.
/// </summary>
public enum TourSchedulePattern : byte
{
    Once   = 0,
    Daily  = 1,
    Weekly = 2,
    Custom = 3,
}
