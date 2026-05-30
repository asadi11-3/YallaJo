using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.RedeemInvitation;

public sealed class RedeemCreatorInvitationCommandValidator
    : AbstractValidator<RedeemCreatorInvitationCommand>
{
    public RedeemCreatorInvitationCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Invitation token is required.")
            .MaximumLength(100);
    }
}
