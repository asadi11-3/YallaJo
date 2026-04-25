using FluentValidation;

namespace ContentCore.Application.Commands.Specialization.DeleteSpecialization;

public sealed class DeleteSpecializationCommandValidator : AbstractValidator<DeleteSpecializationCommand>
{
    public DeleteSpecializationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
