using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.GuideAvailabilityBlock;

internal sealed class GetMyAvailabilityBlocksQueryHandler(
    IGuideAvailabilityBlockRepository repository,
    ITourGuideRepository guideRepository,
    ILogger<GetMyAvailabilityBlocksQueryHandler> logger) : IQueryHandler<GetMyAvailabilityBlocksQuery, IReadOnlyList<GuideAvailabilityBlockDto>>
{
    public async Task<Result<IReadOnlyList<GuideAvailabilityBlockDto>>> Handle(GetMyAvailabilityBlocksQuery request, CancellationToken cancellationToken)
    {
        var guide = await guideRepository.GetByUserIdAsync(request.GuideUserId, cancellationToken);
        if (guide is null)
            return Result<IReadOnlyList<GuideAvailabilityBlockDto>>.Failure(
                new Error("TourGuide.NotFound", "Tour guide not found."), Outcome.NotFound);

        var blocks = await repository.GetByGuideIdAsync(guide.Id, cancellationToken);
        IReadOnlyList<GuideAvailabilityBlockDto> dtos = blocks
            .Select(b => new GuideAvailabilityBlockDto(b.Id, b.StartDate, b.EndDate, b.Reason, b.CreatedAt))
            .ToList();

        logger.LogInformation("Returned {Count} availability blocks for guide {GuideId}", dtos.Count, guide.Id);
        return Result.Success(dtos);
    }
}
