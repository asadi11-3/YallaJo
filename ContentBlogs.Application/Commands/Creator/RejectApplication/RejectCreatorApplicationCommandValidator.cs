using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.RejectApplication;

public sealed class RejectCreatorApplicationCommandValidator
    : AbstractValidator<RejectCreatorApplicationCommand>
{
    public RejectCreatorApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(2000);
    }
}
