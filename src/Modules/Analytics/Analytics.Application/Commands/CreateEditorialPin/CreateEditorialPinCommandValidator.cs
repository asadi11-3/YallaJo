using FluentValidation;

namespace Analytics.Application.Commands.CreateEditorialPin;

public sealed class CreateEditorialPinCommandValidator : AbstractValidator<CreateEditorialPinCommand>
{
    public CreateEditorialPinCommandValidator()
    {
        RuleFor(x => x.EntityKind).IsInEnum();
    }
}
