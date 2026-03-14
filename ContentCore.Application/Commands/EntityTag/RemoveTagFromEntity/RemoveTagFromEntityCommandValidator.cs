using FluentValidation;

namespace ContentCore.Application.Commands.EntityTag.RemoveTagFromEntity;

public sealed class RemoveTagFromEntityCommandValidator : AbstractValidator<RemoveTagFromEntityCommand>
{
    public RemoveTagFromEntityCommandValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty()
            .Must(v => Enum.TryParse<ContentCore.Domain.Enums.EntityType>(v, true, out _))
            .WithMessage("Invalid entity type");

        RuleFor(x => x.EntityId)
            .NotEmpty();

        RuleFor(x => x.TagId)
            .NotEmpty();
    }
}
