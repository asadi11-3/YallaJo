using FluentValidation;

namespace ContentTours.Application.Commands.TourProposal.Submit;

public sealed class SubmitTourProposalCommandValidator : AbstractValidator<SubmitTourProposalCommand>
{
    public SubmitTourProposalCommandValidator()
    {
        RuleFor(x => x.ProposalId).NotEmpty();
    }
}
