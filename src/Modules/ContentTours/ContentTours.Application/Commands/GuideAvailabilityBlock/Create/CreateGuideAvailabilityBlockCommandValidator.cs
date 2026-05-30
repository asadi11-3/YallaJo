using FluentValidation;

namespace ContentTours.Application.Commands.GuideAvailabilityBlock.Create;

public sealed class CreateGuideAvailabilityBlockCommandValidator : AbstractValidator<CreateGuideAvailabilityBlockCommand>
{
    public CreateGuideAvailabilityBlockCommandValidator()
    {
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
