using ContentTours.Application.Queries.TourProposal.Common;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourProposal.ListTourProposals;

public sealed class ListTourProposalsQueryHandler(
    ITourProposalRepository proposalRepository,
    ILogger<ListTourProposalsQueryHandler> logger)
    : IQueryHandler<ListTourProposalsQuery, IReadOnlyList<TourProposalDto>>
{
    public async Task<Result<IReadOnlyList<TourProposalDto>>> Handle(
        ListTourProposalsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.GuideId.HasValue)
        {
            var byGuide = await proposalRepository.GetByGuideIdAsync(
                request.GuideId.Value, request.Status, cancellationToken);
            var dtos = byGuide.Select(TourProposalDto.From).ToList();
            logger.LogInformation(
                "Fetched {Count} tour proposals for guide {GuideId} (status={Status})",
                dtos.Count, request.GuideId, request.Status);
            return Result<IReadOnlyList<TourProposalDto>>.Success(dtos);
        }

        var pending = await proposalRepository.GetPendingAsync(cancellationToken);
        var pendingDtos = pending.Select(TourProposalDto.From).ToList();
        logger.LogInformation("Fetched {Count} pending tour proposals (admin queue)", pendingDtos.Count);
        return Result<IReadOnlyList<TourProposalDto>>.Success(pendingDtos);
    }
}
