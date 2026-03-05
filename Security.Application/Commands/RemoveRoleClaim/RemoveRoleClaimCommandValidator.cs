using FluentValidation;

namespace Security.Application.Commands.RemoveRoleClaim;

public sealed class RemoveRoleClaimCommandValidator : AbstractValidator<RemoveRoleClaimCommand>
{
    public RemoveRoleClaimCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("Role ID is required.");
        RuleFor(x => x.ClaimId).NotEmpty().WithMessage("Claim ID is required.");
    }
}
