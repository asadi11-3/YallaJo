using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.Posts.DemoteTier;

public sealed class DemoteCreatorTierCommandValidator : AbstractValidator<DemoteCreatorTierCommand>
{
    public DemoteCreatorTierCommandValidator()
    {
        RuleFor(x => x.ProfileId).NotEmpty();
        RuleFor(x => x.TargetTier).IsInEnum();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Demotion reason is required.")
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");
    }
}
