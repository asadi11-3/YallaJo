using FluentValidation;

namespace Analytics.Application.Commands.CreateBoostPackage;

public sealed class CreateBoostPackageCommandValidator : AbstractValidator<CreateBoostPackageCommand>
{
    public CreateBoostPackageCommandValidator()
    {
        RuleFor(x => x.EntityKind).IsInEnum();
    }
}
