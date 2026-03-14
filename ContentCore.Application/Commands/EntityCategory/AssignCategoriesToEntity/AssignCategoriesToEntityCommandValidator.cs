using FluentValidation;

namespace ContentCore.Application.Commands.EntityCategory.AssignCategoriesToEntity;

public sealed class AssignCategoriesToEntityCommandValidator : AbstractValidator<AssignCategoriesToEntityCommand>
{
    public AssignCategoriesToEntityCommandValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty()
            .Must(v => Enum.TryParse<ContentCore.Domain.Enums.EntityType>(v, true, out _))
            .WithMessage("Invalid entity type");

        RuleFor(x => x.EntityId)
            .NotEmpty();

        RuleFor(x => x.CategoryIds)
            .NotEmpty();
    }
}
