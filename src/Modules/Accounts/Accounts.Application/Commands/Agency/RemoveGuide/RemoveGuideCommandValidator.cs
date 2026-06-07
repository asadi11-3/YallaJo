using FluentValidation;

namespace Accounts.Application.Commands.Agency.RemoveGuide;

// Sibling of the F126 fix (see InviteGuideCommandValidator): RemoveGuideCommand had no
// validator, so a DELETE /api/v1/agency/guides/{guideUserId} body with a null/empty Reason
// reached the handler and threw an unguarded NullReferenceException
// (AgencyAffiliation.Terminate called reason.Trim() on null) -> 500. This validator returns a
// clean 400 and enforces the "reason required" rule at the API contract layer.
public sealed class RemoveGuideCommandValidator : AbstractValidator<RemoveGuideCommand>
{
    public RemoveGuideCommandValidator()
    {
        RuleFor(x => x.GuideUserId)
            .NotEmpty().WithMessage("GuideUserId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required.")
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");
    }
}
