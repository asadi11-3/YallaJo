using FluentValidation;

namespace ContentCore.Application.Commands.Tag.DeactivateTag;

public sealed class DeactivateTagCommandValidator : AbstractValidator<DeactivateTagCommand>
{
    public DeactivateTagCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
