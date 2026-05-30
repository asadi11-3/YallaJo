using FluentValidation;

namespace ContentCore.Application.Commands.EntityCategory.RemoveCategoryFromEntity;

public sealed class RemoveCategoryFromEntityCommandValidator : AbstractValidator<RemoveCategoryFromEntityCommand>
{
    public RemoveCategoryFromEntityCommandValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty()
            .Must(v => Enum.TryParse<ContentCore.Domain.Enums.EntityType>(v, true, out _))
            .WithMessage("Invalid entity type");

        RuleFor(x => x.EntityId)
            .NotEmpty();

        RuleFor(x => x.CategoryId)
            .NotEmpty();
    }
}
