using FluentValidation;

namespace Security.Application.Commands.AddRoleClaim;

public sealed class AddRoleClaimCommandValidator : AbstractValidator<AddRoleClaimCommand>
{
    public AddRoleClaimCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("Role ID is required.");
        RuleFor(x => x.ClaimType).NotEmpty().WithMessage("Claim type is required.");
        RuleFor(x => x.ClaimValue).NotEmpty().WithMessage("Claim value is required.");
    }
}
