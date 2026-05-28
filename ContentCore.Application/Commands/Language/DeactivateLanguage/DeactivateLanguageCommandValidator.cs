using FluentValidation;

namespace ContentCore.Application.Commands.Language.DeactivateLanguage;

public sealed class DeactivateLanguageCommandValidator : AbstractValidator<DeactivateLanguageCommand>
{
    public DeactivateLanguageCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
