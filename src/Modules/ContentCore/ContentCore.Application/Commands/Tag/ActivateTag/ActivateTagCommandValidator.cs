using FluentValidation;

namespace ContentCore.Application.Commands.Tag.ActivateTag;

public sealed class ActivateTagCommandValidator : AbstractValidator<ActivateTagCommand>
{
    public ActivateTagCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
