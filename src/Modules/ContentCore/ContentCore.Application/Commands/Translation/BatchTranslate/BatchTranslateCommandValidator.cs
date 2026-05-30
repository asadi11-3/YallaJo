using FluentValidation;

namespace ContentCore.Application.Commands.Translation.BatchTranslate;

public sealed class BatchTranslateCommandValidator : AbstractValidator<BatchTranslateCommand>
{
    public BatchTranslateCommandValidator()
    {
        RuleFor(x => x.Texts)
            .NotEmpty()
            .Must(t => t.Count <= 100)
            .WithMessage("Batch size must not exceed 100 texts.");

        RuleForEach(x => x.Texts)
            .NotEmpty()
            .MaximumLength(10_000);

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
