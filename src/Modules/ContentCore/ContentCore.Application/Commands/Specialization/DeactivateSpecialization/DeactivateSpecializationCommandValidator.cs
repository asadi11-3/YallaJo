using FluentValidation;

namespace ContentCore.Application.Commands.Specialization.DeactivateSpecialization;

public sealed class DeactivateSpecializationCommandValidator : AbstractValidator<DeactivateSpecializationCommand>
{
    public DeactivateSpecializationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
