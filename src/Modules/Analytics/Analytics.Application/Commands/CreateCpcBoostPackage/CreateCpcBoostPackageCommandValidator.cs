using FluentValidation;

namespace Analytics.Application.Commands.CreateCpcBoostPackage;

public sealed class CreateCpcBoostPackageCommandValidator : AbstractValidator<CreateCpcBoostPackageCommand>
{
    public CreateCpcBoostPackageCommandValidator()
    {
        RuleFor(x => x.EntityKind).IsInEnum();
    }
}
