using FluentValidation;

namespace ContentCore.Application.Commands.Translation.TriggerTranslationBackfill;

public sealed class TriggerTranslationBackfillCommandValidator
    : AbstractValidator<TriggerTranslationBackfillCommand>
{
    private static readonly HashSet<string> ValidKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "tag", "specialization"
    };

    public TriggerTranslationBackfillCommandValidator()
    {
        RuleFor(x => x.EntityKind)
            .NotEmpty().WithMessage("EntityKind is required.")
            .Must(k => ValidKinds.Contains(k))
            .WithMessage("EntityKind must be 'tag' or 'specialization'.");
    }
}
