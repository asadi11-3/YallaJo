using FluentValidation;

namespace Analytics.Application.Queries.GetUserPreferences;

public sealed class GetUserPreferencesQueryValidator : AbstractValidator<GetUserPreferencesQuery>
{
    public GetUserPreferencesQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
