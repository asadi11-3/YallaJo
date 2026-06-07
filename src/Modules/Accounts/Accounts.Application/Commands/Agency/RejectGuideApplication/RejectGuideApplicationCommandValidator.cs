using FluentValidation;

namespace Accounts.Application.Commands.Agency.RejectGuideApplication;

// Sibling of the F126 fix (see InviteGuideCommandValidator): RejectGuideApplicationCommand
// had no validator, so a POST /api/v1/agency/applications/{id}/reject body with a null/empty
// Reason reached the handler and threw an unguarded NullReferenceException
// (AgencyApplication.Reject called reason.Trim() on null) -> 500. This validator returns a
// clean 400 and enforces the "reason required" rule at the API contract layer (the web BFF
// already requires it, but the API is a public contract and must validate independently).
public sealed class RejectGuideApplicationCommandValidator : AbstractValidator<RejectGuideApplicationCommand>
{
    public RejectGuideApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required.")
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");
    }
}
