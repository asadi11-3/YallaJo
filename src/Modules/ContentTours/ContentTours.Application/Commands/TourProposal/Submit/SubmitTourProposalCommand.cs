using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.TourProposal.Submit;

public sealed record SubmitTourProposalCommand(Guid ProposalId) : ICommand;
