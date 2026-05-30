using FluentValidation;

namespace ContentTours.Application.Commands.GuideAvailabilityBlock.Delete;

public sealed class DeleteGuideAvailabilityBlockCommandValidator : AbstractValidator<DeleteGuideAvailabilityBlockCommand>
{
    public DeleteGuideAvailabilityBlockCommandValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty();
    }
}
