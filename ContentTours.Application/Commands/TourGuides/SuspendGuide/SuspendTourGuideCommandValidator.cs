using FluentValidation;

namespace ContentTours.Application.Commands.TourGuides.SuspendGuide;

public sealed class SuspendTourGuideCommandValidator : AbstractValidator<SuspendTourGuideCommand>
{
    public SuspendTourGuideCommandValidator()
    {
        RuleFor(x => x.TourGuideId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
