// Security.Application/Commands/AssignRole/AssignRoleCommandValidator.cs
using FluentValidation;

namespace Security.Application.Commands.AssignRole;

public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("Role ID is required.");
    }
}
