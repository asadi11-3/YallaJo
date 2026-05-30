using FluentValidation;

namespace Social.Application.Commands.RemoveFavorite;

internal sealed class RemoveFavoriteCommandValidator : AbstractValidator<RemoveFavoriteCommand>
{
    public RemoveFavoriteCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.EntityType).IsInEnum();
        RuleFor(x => x.EntityId).NotEmpty();
    }
}
