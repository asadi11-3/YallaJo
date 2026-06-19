using ContentTours.Application.Queries.TourGuides.Common;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuides;

public sealed class GetTourGuidesQueryHandler(
    ITourTourGuideRepository guideRepo,
    ITourGuideRepository guideProfileRepository,
    ILogger<GetTourGuidesQueryHandler> logger)
    : IQueryHandler<GetTourGuidesQuery, IReadOnlyCollection<TourGuideDto>>
{
    public async Task<Result<IReadOnlyCollection<TourGuideDto>>> Handle(
        GetTourGuidesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var guides = await guideRepo
                .Query(filter: g => g.TourId == request.TourId)
                .OrderByDescending(g => g.IsPrimary)
                .ThenBy(g => g.TourGuideId)
                .ToListAsync(cancellationToken);

            // Batch-load the TourGuide profiles for the assigned guides (no N+1).
            // TourTourGuide.TourGuideId stores the guide's UserId.
            var userIds = guides.Select(g => g.TourGuideId).Distinct().ToList();
            var profiles = await guideProfileRepository
                .GetAllAsync(filter: g => userIds.Contains(g.UserId), ct: cancellationToken)
                .ConfigureAwait(false);
            var byUserId = profiles.ToDictionary(p => p.UserId);

            var dtos = new List<TourGuideDto>(guides.Count);
            foreach (var guide in guides)
            {
                var hasProfile = byUserId.TryGetValue(guide.TourGuideId, out var gp);

                dtos.Add(new TourGuideDto(
                    TourGuideId: guide.TourGuideId,
                    IsPrimary:   guide.IsPrimary,
                    DisplayName: hasProfile ? gp!.DisplayName : guide.TourGuideId.ToString(),
                    AvatarUrl:   hasProfile ? gp!.AvatarUrl : null));
            }

            logger.LogDebug(
                "Listed {Count} guides for TourId={TourId}",
                dtos.Count, request.TourId);

            return Result.Success<IReadOnlyCollection<TourGuideDto>>(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyCollection<TourGuideDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
