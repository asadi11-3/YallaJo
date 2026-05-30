// Security.Application/Commands/CreateRole/CreateRoleCommandValidator.cs
using FluentValidation;
using Security.Contracts.Authorization;

namespace Security.Application.Commands.CreateRole;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_]*$")
                .WithMessage("Role name must start with a letter and contain only letters, digits, and underscores.")
            .Must(name => !AppRoles.ProtectedRoles.Contains(name, StringComparer.OrdinalIgnoreCase))
                .WithMessage("That role name is reserved and cannot be created.");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("Description must not exceed 200 characters.")
            .When(x => x.Description is not null);
    }
}
