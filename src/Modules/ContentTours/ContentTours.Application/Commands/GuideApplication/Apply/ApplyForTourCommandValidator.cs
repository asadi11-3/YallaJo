using FluentValidation;

namespace ContentTours.Application.Commands.GuideApplication.Apply;

public sealed class ApplyForTourCommandValidator : AbstractValidator<ApplyForTourCommand>
{
    public ApplyForTourCommandValidator()
    {
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RelevantExperience).MaximumLength(2000);
        RuleFor(x => x.ProposedBasePrice)
            .GreaterThan(0).When(x => x.ProposedBasePrice.HasValue);
    }
}
