using FluentValidation;

namespace Security.Application.Commands.AddUserClaim;

public sealed class AddUserClaimCommandValidator : AbstractValidator<AddUserClaimCommand>
{
    public AddUserClaimCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.ClaimType).NotEmpty().WithMessage("Claim type is required.");
        RuleFor(x => x.ClaimValue).NotEmpty().WithMessage("Claim value is required.");
    }
}
