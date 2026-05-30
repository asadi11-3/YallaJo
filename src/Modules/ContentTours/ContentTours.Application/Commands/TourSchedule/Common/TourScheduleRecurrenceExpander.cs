using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourSchedule.Common;

internal static class TourScheduleRecurrenceExpander
{
    /// <summary>Hard cap on candidate row count produced by a single expansion call (PDF B2).</summary>
    public const int MaxCandidates = 120;

    /// <summary>Hard cap on the validity window (PDF B2): 90 days from today UTC.</summary>
    public const int MaxValidityDays = 90;

    public static Result<IReadOnlyList<TourScheduleCandidate>> Expand(
        TourSchedulePattern pattern,
        IReadOnlyList<byte>? daysOfWeek,
        IReadOnlyList<DateOnly>? customDates,
        TimeOnly startTime,
        TimeOnly? endTime,
        DateOnly? validFrom,
        DateOnly? validTo)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveFrom = validFrom ?? today;
        var effectiveTo = validTo ?? effectiveFrom.AddDays(MaxValidityDays);

        // 90-day hard cap measured from today UTC, not from validFrom.
        var cap = today.AddDays(MaxValidityDays);

        if (effectiveTo > cap)
        {
            effectiveTo = cap;
        }

        switch (pattern)
        {
            case TourSchedulePattern.Once:
                {
                    var date = customDates is { Count: > 0 }
                        ? customDates[0]
                        : effectiveFrom;

                    if (date < today || date > cap)
                    {
                        return Result<IReadOnlyList<TourScheduleCandidate>>.Failure(
                            new Error(
                                "TourSchedule.CustomDateOutOfRange",
                                $"Once pattern date {date:yyyy-MM-dd} must be within [{today:yyyy-MM-dd}, {cap:yyyy-MM-dd}]."),
                            Outcome.UnprocessableEntity);
                    }

                    return Ok(new[]
                    {
                    new TourScheduleCandidate((byte)date.DayOfWeek, startTime, endTime)
                    });
                }

            case TourSchedulePattern.Daily:
                {
                    // ValidFrom MUST lie inside the 90-day scheduling window.
                    // Without this guard the window-collapse fallback (effectiveTo < effectiveFrom)
                    // would emit a single candidate dated effectiveFrom, leaking a row beyond the cap.
                    if (effectiveFrom < today || effectiveFrom > cap)
                    {
                        return Result<IReadOnlyList<TourScheduleCandidate>>.Failure(
                            new Error(
                                "TourSchedule.PatternParamsInvalid",
                                $"ValidFrom {effectiveFrom:yyyy-MM-dd} must be within the 90-day scheduling window " +
                                $"[{today:yyyy-MM-dd}, {cap:yyyy-MM-dd}]."),
                            Outcome.Invalid);
                    }

                    var rows = new List<TourScheduleCandidate>();

                    for (var d = effectiveFrom; d <= effectiveTo; d = d.AddDays(1))
                    {
                        rows.Add(new TourScheduleCandidate((byte)d.DayOfWeek, startTime, endTime));

                        if (rows.Count > MaxCandidates)
                        {
                            return TooLarge(rows.Count);
                        }
                    }

                    return Ok(rows);
                }

            case TourSchedulePattern.Weekly:
                {
                    if (daysOfWeek is null || daysOfWeek.Count == 0)
                    {
                        return Result<IReadOnlyList<TourScheduleCandidate>>.Failure(
                            new Error(
                                "TourSchedule.PatternParamsInvalid",
                                "Weekly pattern requires DaysOfWeek to be non-empty."),
                            Outcome.Invalid);
                    }

                    if (daysOfWeek.Any(b => b > 6))
                    {
                        return Result<IReadOnlyList<TourScheduleCandidate>>.Failure(
                            new Error(
                                "TourSchedule.InvalidDayOfWeek",
                                "Each DayOfWeek must be 0-6 (Sunday=0, Saturday=6)."),
                            Outcome.Invalid);
                    }

                    if (effectiveFrom < today || effectiveFrom > cap)
                    {
                        return Result<IReadOnlyList<TourScheduleCandidate>>.Failure(
                            new Error(
                                "TourSchedule.PatternParamsInvalid",
                                $"ValidFrom {effectiveFrom:yyyy-MM-dd} must be within the 90-day scheduling window " +
                                $"[{today:yyyy-MM-dd}, {cap:yyyy-MM-dd}]."),
                            Outcome.Invalid);
                    }

                    var dowSet = daysOfWeek.Distinct().ToHashSet();
                    var rows = new List<TourScheduleCandidate>();

                    for (var d = effectiveFrom; d <= effectiveTo; d = d.AddDays(1))
                    {
                        if (!dowSet.Contains((byte)d.DayOfWeek))
                        {
                            continue;
                        }

                        rows.Add(new TourScheduleCandidate((byte)d.DayOfWeek, startTime, endTime));

                        if (rows.Count > MaxCandidates)
                        {
                            return TooLarge(rows.Count);
                        }
                    }

                    return Ok(rows);
                }

            case TourSchedulePattern.Custom:
                {
                    if (customDates is null || customDates.Count == 0)
                    {
                        return Result<IReadOnlyList<TourScheduleCandidate>>.Failure(
                            new Error(
                                "TourSchedule.PatternParamsInvalid",
                                "Custom pattern requires CustomDates to be non-empty."),
                            Outcome.Invalid);
                    }

                    foreach (var d in customDates)
                    {
                        if (d < today || d > cap)
                        {
                            return Result<IReadOnlyList<TourScheduleCandidate>>.Failure(
                                new Error(
                                    "TourSchedule.CustomDateOutOfRange",
                                    $"Custom date {d:yyyy-MM-dd} must be within [{today:yyyy-MM-dd}, {cap:yyyy-MM-dd}]."),
                                Outcome.UnprocessableEntity);
                        }
                    }

                    if (customDates.Count > MaxCandidates)
                    {
                        return TooLarge(customDates.Count);
                    }

                    var rows = customDates
                        .Select(d => new TourScheduleCandidate((byte)d.DayOfWeek, startTime, endTime))
                        .ToList();

                    return Ok(rows);
                }

            default:
                {
                    return Result<IReadOnlyList<TourScheduleCandidate>>.Failure(
                        new Error(
                            "TourSchedule.PatternParamsInvalid",
                            $"Unsupported pattern '{pattern}'."),
                        Outcome.Invalid);
                }
        }
    }

    private static Result<IReadOnlyList<TourScheduleCandidate>> Ok(IReadOnlyList<TourScheduleCandidate> rows)
        => Result.Success(rows);

    private static Result<IReadOnlyList<TourScheduleCandidate>> TooLarge(int count)
        => Result<IReadOnlyList<TourScheduleCandidate>>.Failure(
            new Error(
                "TourSchedule.ExpansionTooLarge",
                $"Recurrence would generate {count} rows; the per-call limit is {MaxCandidates}. " +
                $"Narrow the validity window or supply fewer CustomDates."),
            Outcome.UnprocessableEntity);
}

/// <summary>One emitted recurrence candidate, prior to overlap and idempotency checks.</summary>
internal readonly record struct TourScheduleCandidate(
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime);
