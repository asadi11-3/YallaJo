using FluentValidation;

namespace ContentTours.Application.Commands.TourGuides.ReinstateGuide;

public sealed class ReinstateTourGuideCommandValidator : AbstractValidator<ReinstateTourGuideCommand>
{
    public ReinstateTourGuideCommandValidator()
    {
        RuleFor(x => x.TourGuideId).NotEmpty();
    }
}
