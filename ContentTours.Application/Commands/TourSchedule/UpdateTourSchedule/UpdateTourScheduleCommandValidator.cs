using FluentValidation;

namespace ContentTours.Application.Commands.TourSchedule.UpdateTourSchedule;

public sealed class UpdateTourScheduleCommandValidator : AbstractValidator<UpdateTourScheduleCommand>
{
    public UpdateTourScheduleCommandValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);
        RuleFor(x => x.ScheduleId).NotEqual(Guid.Empty);
        RuleFor(x => x.DayOfWeek).InclusiveBetween((byte)0, (byte)6);
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("EndTime must be after StartTime.")
            .When(x => x.EndTime.HasValue);
    }
}
