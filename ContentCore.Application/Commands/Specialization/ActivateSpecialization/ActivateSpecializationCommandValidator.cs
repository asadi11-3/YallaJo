using FluentValidation;

namespace ContentCore.Application.Commands.Specialization.ActivateSpecialization;

public sealed class ActivateSpecializationCommandValidator : AbstractValidator<ActivateSpecializationCommand>
{
    public ActivateSpecializationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
