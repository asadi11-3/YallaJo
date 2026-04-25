using FluentValidation;

namespace ContentCore.Application.Commands.Translation.BatchApproveTranslations;

public sealed class BatchApproveTranslationsCommandValidator
    : AbstractValidator<BatchApproveTranslationsCommand>
{
    public BatchApproveTranslationsCommandValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("EntityType is required.");

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("EntityId is required.");

        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("LanguageCode is required.")
            .MaximumLength(10).WithMessage("LanguageCode must be 10 characters or fewer.");
    }
}
