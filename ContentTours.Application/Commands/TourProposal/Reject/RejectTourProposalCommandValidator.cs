using FluentValidation;

namespace ContentTours.Application.Commands.TourProposal.Reject;

public sealed class RejectTourProposalCommandValidator : AbstractValidator<RejectTourProposalCommand>
{
    public RejectTourProposalCommandValidator()
    {
        RuleFor(x => x.ProposalId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
