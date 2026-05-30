using FluentValidation;

namespace Analytics.Application.Commands.SetUserPreferences;

public sealed class SetUserPreferencesCommandValidator : AbstractValidator<SetUserPreferencesCommand>
{
    public SetUserPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleForEach(x => x.Categories).ChildRules(category =>
        {
            category.RuleFor(x => x.CategoryId).NotEmpty();
            category.RuleFor(x => x.PreferenceScore).InclusiveBetween(0m, 1m);
        });
    }
}
