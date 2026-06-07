using FluentValidation;

namespace Accounts.Application.Commands.Agency.ApplyToAgency;

// Sibling of the F126 fix (see InviteGuideCommandValidator): ApplyToAgencyCommand had no
// validator. Message is optional (the apply form labels it "optional" and sends null for
// blank input), so the null case is handled in AgencyApplication.Create via null-coalesce;
// this validator only guards the required AgencyUserId and caps Message length, returning a
// clean 400 instead of letting bad input reach the handler.
public sealed class ApplyToAgencyCommandValidator : AbstractValidator<ApplyToAgencyCommand>
{
    public ApplyToAgencyCommandValidator()
    {
        RuleFor(x => x.AgencyUserId)
            .NotEmpty().WithMessage("AgencyUserId is required.");

        RuleFor(x => x.Message)
            .MaximumLength(1000).WithMessage("Message must not exceed 1000 characters.")
            .When(x => x.Message is not null);
    }
}
