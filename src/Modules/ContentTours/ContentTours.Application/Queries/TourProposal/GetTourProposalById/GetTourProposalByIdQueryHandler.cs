using ContentTours.Application.Queries.TourProposal.Common;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourProposal.GetTourProposalById;

public sealed class GetTourProposalByIdQueryHandler(
    ITourProposalRepository proposalRepository,
    ILogger<GetTourProposalByIdQueryHandler> logger)
    : IQueryHandler<GetTourProposalByIdQuery, TourProposalDto>
{
    public async Task<Result<TourProposalDto>> Handle(
        GetTourProposalByIdQuery request,
        CancellationToken cancellationToken)
    {
        var proposal = await proposalRepository.GetWithDetailsAsync(request.Id, cancellationToken);
        if (proposal is null)
        {
            return Result<TourProposalDto>.Failure(
                new Error("TourProposal.NotFound", $"Tour proposal '{request.Id}' was not found."),
                Outcome.NotFound);
        }

        logger.LogInformation("Fetched tour proposal {ProposalId} (status {Status})", proposal.Id, proposal.Status);
        return Result<TourProposalDto>.Success(TourProposalDto.From(proposal));
    }
}
