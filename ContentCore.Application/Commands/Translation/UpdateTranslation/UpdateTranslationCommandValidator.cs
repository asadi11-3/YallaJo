using FluentValidation;

namespace ContentCore.Application.Commands.Translation.UpdateTranslation;

public sealed class UpdateTranslationCommandValidator : AbstractValidator<UpdateTranslationCommand>
{
    public UpdateTranslationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.TranslatedText)
            .NotEmpty()
            .MaximumLength(10_000);
    }
}
