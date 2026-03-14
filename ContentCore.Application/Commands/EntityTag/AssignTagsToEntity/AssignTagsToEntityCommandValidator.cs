using FluentValidation;

namespace ContentCore.Application.Commands.EntityTag.AssignTagsToEntity;

public sealed class AssignTagsToEntityCommandValidator : AbstractValidator<AssignTagsToEntityCommand>
{
    public AssignTagsToEntityCommandValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty()
            .Must(v => Enum.TryParse<ContentCore.Domain.Enums.EntityType>(v, true, out _))
            .WithMessage("Invalid entity type");

        RuleFor(x => x.EntityId)
            .NotEmpty();

        RuleFor(x => x.TagIds)
            .NotEmpty();
    }
}
