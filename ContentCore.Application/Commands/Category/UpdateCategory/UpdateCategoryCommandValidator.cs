using FluentValidation;

namespace ContentCore.Application.Commands.Category.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(200)
            .Matches(@"^[a-z0-9\-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens.");

        RuleFor(x => x.Icon)
            .MaximumLength(100)
            .When(x => x.Icon is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SortOrder.HasValue);

        RuleFor(x => x.SourceLanguageCode)
            .NotEmpty()
            .MaximumLength(10);

        // Validate ParentCategoryId if provided
        RuleFor(x => x.ParentCategoryId)
            .NotEqual(Guid.Empty)
            .When(x => x.ParentCategoryId.HasValue);

        // Validate Translations if provided
        RuleForEach(x => x.Translations)
            .ChildRules(translation =>
            {
                translation.RuleFor(t => t.LanguageId)
                    .NotEmpty();

                translation.RuleFor(t => t.Name)
                    .NotEmpty()
                    .MaximumLength(200);

                translation.RuleFor(t => t.Slug)
                    .NotEmpty()
                    .MaximumLength(200)
                    .Matches(@"^[a-z0-9\-]+$");
            });
    }
}