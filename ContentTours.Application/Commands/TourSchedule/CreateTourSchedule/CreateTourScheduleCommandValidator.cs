using FluentValidation;

namespace ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;

public sealed class CreateTourScheduleCommandValidator : AbstractValidator<CreateTourScheduleCommand>
{
    public CreateTourScheduleCommandValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);

        RuleFor(x => x.DaysOfWeek)
            .NotEmpty().WithMessage("At least one DayOfWeek is required.");

        RuleForEach(x => x.DaysOfWeek)
            .InclusiveBetween((byte)0, (byte)6)
            .WithMessage("Each DayOfWeek must be 0–6 (Sunday=0, Saturday=6).");

        RuleFor(x => x.StartTime).NotEmpty();

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("EndTime must be after StartTime.")
            .When(x => x.EndTime.HasValue);
    }
}
