using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.TourProposal.Reject;

public sealed record RejectTourProposalCommand(Guid ProposalId, string Reason) : ICommand;
