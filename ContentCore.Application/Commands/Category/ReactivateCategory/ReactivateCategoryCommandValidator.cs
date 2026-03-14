using FluentValidation;

namespace ContentCore.Application.Commands.Category.ReactivateCategory;

public sealed class ReactivateCategoryCommandValidator : AbstractValidator<ReactivateCategoryCommand>
{
    public ReactivateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
