using FluentValidation;

namespace ContentCore.Application.Commands.Category.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .MaximumLength(200)
            .Matches(@"^[a-z0-9\-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens.")
            .When(x => x.Slug is not null);

        RuleFor(x => x.Icon)
            .MaximumLength(100)
            .When(x => x.Icon is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.SourceLanguageCode)
            .NotEmpty()
            .MaximumLength(10);
    }
}
