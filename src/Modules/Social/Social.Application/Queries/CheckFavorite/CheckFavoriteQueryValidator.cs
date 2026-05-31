using FluentValidation;

namespace Social.Application.Queries.CheckFavorite;

public sealed class CheckFavoriteQueryValidator : AbstractValidator<CheckFavoriteQuery>
{
    public CheckFavoriteQueryValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
    }
}
