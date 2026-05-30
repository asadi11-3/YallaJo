using FluentValidation;

namespace Security.Application.Commands.RemoveUserClaim;

public sealed class RemoveUserClaimCommandValidator : AbstractValidator<RemoveUserClaimCommand>
{
    public RemoveUserClaimCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.ClaimId).NotEmpty().WithMessage("Claim ID is required.");
    }
}
