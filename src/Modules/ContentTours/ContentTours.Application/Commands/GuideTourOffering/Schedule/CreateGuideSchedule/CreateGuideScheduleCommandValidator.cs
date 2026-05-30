using FluentValidation;

namespace ContentTours.Application.Commands.GuideTourOffering.Schedule.CreateGuideSchedule;

public sealed class CreateGuideScheduleCommandValidator : AbstractValidator<CreateGuideScheduleCommand>
{
    public CreateGuideScheduleCommandValidator()
    {
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.TourGuideId).NotEmpty();
        RuleFor(x => x.DayOfWeek).InclusiveBetween((byte)0, (byte)6);
        RuleFor(x => x.StartTime).NotEmpty();
    }
}
