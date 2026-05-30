using FluentValidation;

namespace ContentTours.Application.Commands.GuideTourOffering.Schedule.UpdateGuideSchedule;

public sealed class UpdateGuideScheduleCommandValidator : AbstractValidator<UpdateGuideScheduleCommand>
{
    public UpdateGuideScheduleCommandValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
        RuleFor(x => x.DayOfWeek).InclusiveBetween((byte)0, (byte)6);
        RuleFor(x => x.StartTime).NotEmpty();
    }
}
