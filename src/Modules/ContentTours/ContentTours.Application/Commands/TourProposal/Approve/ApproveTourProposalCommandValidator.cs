using FluentValidation;

namespace ContentTours.Application.Commands.TourProposal.Approve;

public sealed class ApproveTourProposalCommandValidator : AbstractValidator<ApproveTourProposalCommand>
{
    public ApproveTourProposalCommandValidator()
    {
        RuleFor(x => x.ProposalId).NotEmpty();
    }
}
