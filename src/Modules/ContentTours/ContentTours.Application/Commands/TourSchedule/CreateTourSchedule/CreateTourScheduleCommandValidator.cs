using ContentTours.Application.Commands.TourSchedule.Common;
using FluentValidation;

namespace ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;

/// <summary>
/// Shape-level validation for <see cref="CreateTourScheduleCommand"/>. Pattern-conditional
/// and date-window rules per PDF Task-2A B2 + Validator Rules section. Pattern-conditional
/// emptiness/range checks (e.g. "Custom date in [today, today+90]") are also enforced inside
/// the expansion engine so they apply to non-validator callers (tests, internal flows).
/// </summary>
public sealed class CreateTourScheduleCommandValidator : AbstractValidator<CreateTourScheduleCommand>
{
    public CreateTourScheduleCommandValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);

        RuleFor(x => x.Pattern)
            .IsInEnum()
            .WithMessage("Pattern must be one of: Once, Daily, Weekly, Custom.");

        // Weekly: DaysOfWeek required and each value 0..6.
        RuleFor(x => x.DaysOfWeek)
            .NotEmpty()
            .WithMessage("Weekly pattern requires at least one DayOfWeek.")
            .When(x => x.Pattern == TourSchedulePattern.Weekly);

        RuleForEach(x => x.DaysOfWeek!)
            .InclusiveBetween((byte)0, (byte)6)
            .WithMessage("Each DayOfWeek must be 0-6 (Sunday=0, Saturday=6).")
            .When(x => x.DaysOfWeek is { Count: > 0 });

        // Custom: CustomDates required and each lies in [today, today+90].
        RuleFor(x => x.CustomDates)
            .NotEmpty()
            .WithMessage("Custom pattern requires at least one CustomDate.")
            .When(x => x.Pattern == TourSchedulePattern.Custom);

        RuleForEach(x => x.CustomDates!)
            .Must(BeWithin90DayWindow)
            .WithMessage("Each CustomDate must be within [today, today+90].")
            .When(x => x.CustomDates is { Count: > 0 });

        // StartTime required (TimeOnly cannot be null but can be default; treat default as invalid).
        RuleFor(x => x.StartTime)
            .NotEqual(default(TimeOnly))
            .WithMessage("StartTime is required.");

        // EndTime optional; if supplied, must be strictly greater than StartTime.
        RuleFor(x => x.EndTime!.Value)
            .GreaterThan(x => x.StartTime)
            .WithMessage("EndTime must be after StartTime.")
            .When(x => x.EndTime.HasValue);

        // ValidTo >= ValidFrom when both supplied.
        RuleFor(x => x.ValidTo!.Value)
            .GreaterThanOrEqualTo(x => x.ValidFrom!.Value)
            .WithMessage("ValidTo must be greater than or equal to ValidFrom.")
            .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue);
    }

    private static bool BeWithin90DayWindow(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cap = today.AddDays(TourScheduleRecurrenceExpander.MaxValidityDays);
        return date >= today && date <= cap;
    }
}
