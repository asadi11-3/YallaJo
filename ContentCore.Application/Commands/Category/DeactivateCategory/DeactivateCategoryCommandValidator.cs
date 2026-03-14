using FluentValidation;

namespace ContentCore.Application.Commands.Category.DeactivateCategory;

public sealed class DeactivateCategoryCommandValidator : AbstractValidator<DeactivateCategoryCommand>
{
    public DeactivateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
