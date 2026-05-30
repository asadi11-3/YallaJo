using FluentValidation;

namespace Messaging.Application.Commands.UpdatePreferences;

internal sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Updates).NotEmpty();
        RuleForEach(x => x.Updates).ChildRules(update =>
        {
            update.RuleFor(u => u.Type).IsInEnum();
            update.RuleFor(u => u.Channel).IsInEnum();
        });
    }
}
