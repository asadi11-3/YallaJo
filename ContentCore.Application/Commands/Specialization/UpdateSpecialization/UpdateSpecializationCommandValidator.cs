using FluentValidation;

namespace ContentCore.Application.Commands.Specialization.UpdateSpecialization;

public sealed class UpdateSpecializationCommandValidator : AbstractValidator<UpdateSpecializationCommand>
{
    public UpdateSpecializationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.Icon).MaximumLength(100).When(x => x.Icon is not null);
    }
}
