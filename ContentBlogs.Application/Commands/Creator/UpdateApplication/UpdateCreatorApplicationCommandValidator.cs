using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.UpdateApplication;

public sealed class UpdateCreatorApplicationCommandValidator
    : AbstractValidator<UpdateCreatorApplicationCommand>
{
    public UpdateCreatorApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Bio!)
            .MaximumLength(2000)
            .When(x => x.Bio is not null);
    }
}
