using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.SuspendProfile;

public sealed class SuspendCreatorProfileCommandValidator
    : AbstractValidator<SuspendCreatorProfileCommand>
{
    public SuspendCreatorProfileCommandValidator()
    {
        RuleFor(x => x.ProfileId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Suspension reason is required.")
            .MaximumLength(2000);
    }
}
