using FluentValidation;

namespace ContentTours.Application.Commands.TourSchedule.DeleteTourSchedule;

public sealed class DeleteTourScheduleCommandValidator : AbstractValidator<DeleteTourScheduleCommand>
{
    public DeleteTourScheduleCommandValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);
        RuleFor(x => x.ScheduleId).NotEqual(Guid.Empty);
    }
}
