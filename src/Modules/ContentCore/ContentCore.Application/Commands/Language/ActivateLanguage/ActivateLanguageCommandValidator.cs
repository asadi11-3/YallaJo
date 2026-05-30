using FluentValidation;

namespace ContentCore.Application.Commands.Language.ActivateLanguage;

public sealed class ActivateLanguageCommandValidator : AbstractValidator<ActivateLanguageCommand>
{
    public ActivateLanguageCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
