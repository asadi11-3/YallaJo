using Booking.Application.Commands.CreateBulkAvailabilitySlots;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Presentation.Endpoints.AvailabilitySlot;

public sealed record CreateBulkAvailabilitySlotsRequest(
    Guid TourId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Recurrence,
    IReadOnlyList<string>? DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    bool? SkipExisting)
{
    public Result<CreateBulkAvailabilitySlotsCommand> ToCommand()
    {
        if (!Enum.TryParse<AvailabilityRecurrence>(Recurrence, ignoreCase: true, out var recurrence))
        {
            return Result.Failure<CreateBulkAvailabilitySlotsCommand>(
                new Error("AvailabilitySlot.InvalidRecurrence", "Recurrence must be Daily, Weekly, or Custom."),
                Outcome.Invalid);
        }

        IReadOnlyList<DayOfWeek>? parsedDays = null;
        if (recurrence == AvailabilityRecurrence.Custom)
        {
            if (DaysOfWeek is null || DaysOfWeek.Count == 0)
            {
                return Result.Failure<CreateBulkAvailabilitySlotsCommand>(
                    new Error("AvailabilitySlot.InvalidDaysOfWeek", "DaysOfWeek is required when recurrence is Custom."),
                    Outcome.Invalid);
            }

            var mapped = new List<DayOfWeek>(DaysOfWeek.Count);
            foreach (var raw in DaysOfWeek)
            {
                if (!TryParseDayOfWeek(raw, out var day))
                {
                    return Result.Failure<CreateBulkAvailabilitySlotsCommand>(
                        new Error("AvailabilitySlot.InvalidDaysOfWeek", $"Invalid day token '{raw}'. Use Mon..Sun."),
                        Outcome.Invalid);
                }

                mapped.Add(day);
            }

            parsedDays = mapped.Distinct().ToList();
        }

        return Result.Success(new CreateBulkAvailabilitySlotsCommand(
            TourId,
            StartDate,
            EndDate,
            recurrence,
            parsedDays,
            StartTime,
            EndTime,
            MaxCapacity,
            SkipExisting ?? true));
    }

    private static bool TryParseDayOfWeek(string raw, out DayOfWeek day)
    {
        day = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = raw.Trim();
        if (Enum.TryParse<DayOfWeek>(normalized, ignoreCase: true, out day))
        {
            return true;
        }

        return normalized.ToLowerInvariant() switch
        {
            "sun" => Set(DayOfWeek.Sunday, out day),
            "mon" => Set(DayOfWeek.Monday, out day),
            "tue" or "tues" => Set(DayOfWeek.Tuesday, out day),
            "wed" => Set(DayOfWeek.Wednesday, out day),
            "thu" or "thur" or "thurs" => Set(DayOfWeek.Thursday, out day),
            "fri" => Set(DayOfWeek.Friday, out day),
            "sat" => Set(DayOfWeek.Saturday, out day),
            _ => false,
        };

        static bool Set(DayOfWeek value, out DayOfWeek output)
        {
            output = value;
            return true;
        }
    }
}
