using Analytics.Domain.Enums;
using FluentValidation;

namespace Analytics.Application.Commands.RecordInteraction;

public sealed class RecordInteractionCommandValidator : AbstractValidator<RecordInteractionCommand>
{
    public RecordInteractionCommandValidator()
    {
        RuleFor(x => x.EntityType).Must(x => Enum.TryParse<EntityType>(x, true, out _));
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.InteractionType).Must(x => Enum.TryParse<InteractionType>(x, true, out _));
    }
}
