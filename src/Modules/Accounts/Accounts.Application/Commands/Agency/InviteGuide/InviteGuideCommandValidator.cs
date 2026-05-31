using FluentValidation;

namespace Accounts.Application.Commands.Agency.InviteGuide;

// F126 fix 2026-05-30: InviteGuideCommand had no validator, so an empty/invalid
// POST /api/v1/agency/guides/invite body reached the handler and threw an
// unguarded NullReferenceException (AgencyInvitation.Create called message.Trim()
// on a null Message) -> 500. This validator returns a clean 400 for invalid input.
public sealed class InviteGuideCommandValidator : AbstractValidator<InviteGuideCommand>
{
    public InviteGuideCommandValidator()
    {
        RuleFor(x => x.GuideUserId)
            .NotEmpty().WithMessage("GuideUserId is required.");

        RuleFor(x => x.ProposedCommissionPercentage)
            .InclusiveBetween(0m, 100m)
            .WithMessage("Proposed commission percentage must be between 0 and 100.");

        RuleFor(x => x.Message)
            .MaximumLength(1000)
            .WithMessage("Message must not exceed 1000 characters.")
            .When(x => x.Message is not null);
    }
}
