using FluentValidation;

namespace ContentCore.Application.Commands.Language.DeleteLanguage;

public sealed class DeleteLanguageCommandValidator : AbstractValidator<DeleteLanguageCommand>
{
    public DeleteLanguageCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
