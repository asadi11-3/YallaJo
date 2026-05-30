using FluentValidation;

namespace ContentCore.Application.Commands.Translation.TranslateText;

public sealed class TranslateTextCommandValidator : AbstractValidator<TranslateTextCommand>
{
    public TranslateTextCommandValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(10_000)
            .WithMessage("Text must not exceed 10,000 characters.");

        RuleFor(x => x.FromLanguageCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.ToLanguageCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x)
            .Must(x => !string.Equals(x.FromLanguageCode, x.ToLanguageCode, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source and target languages must be different.");
    }
}
