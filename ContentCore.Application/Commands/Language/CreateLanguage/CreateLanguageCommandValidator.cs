using FluentValidation;

namespace ContentCore.Application.Commands.Language.CreateLanguage;

public sealed class CreateLanguageCommandValidator : AbstractValidator<CreateLanguageCommand>
{
    public CreateLanguageCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(10)
            .Matches("^[a-zA-Z]{2,10}$")
            .WithMessage("Language code must be 2-10 alphabetic characters (ISO 639-1).");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.NativeName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
