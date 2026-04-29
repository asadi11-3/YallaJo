using FluentValidation;

namespace ContentTours.Application.Commands.TourGuides.Assign;

public sealed class AssignTourGuideCommandValidator : AbstractValidator<AssignTourGuideCommand>
{
    public AssignTourGuideCommandValidator()
    {
        RuleFor(x => x.TourId)
            .NotEmpty();

        RuleFor(x => x.TourGuideUserId)
            .NotEmpty()
            .NotEqual(Guid.Empty)
            .WithMessage("TourGuideUserId cannot be empty.");
    }
}
