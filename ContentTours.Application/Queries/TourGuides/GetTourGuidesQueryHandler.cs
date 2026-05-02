using ContentTours.Application.Interfaces;
using ContentTours.Application.Queries.TourGuides.Common;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuides;

public sealed class GetTourGuidesQueryHandler(
    ITourTourGuideRepository guideRepo,
    IProfileLookupService profileLookup,
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

            var dtos = new List<TourGuideDto>(guides.Count);
            foreach (var guide in guides)
            {
                var profile = await profileLookup.GetPublicProfileAsync(
                    guide.TourGuideId, cancellationToken);

                dtos.Add(new TourGuideDto(
                    TourGuideId: guide.TourGuideId,
                    IsPrimary:   guide.IsPrimary,
                    DisplayName: profile?.DisplayName ?? guide.TourGuideId.ToString(),
                    AvatarUrl:   profile?.AvatarUrl));
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
