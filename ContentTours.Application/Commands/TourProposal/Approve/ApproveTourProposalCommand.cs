using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.TourProposal.Approve;

public sealed record ApproveTourProposalCommand(Guid ProposalId, bool IsExclusive) : ICommand;
