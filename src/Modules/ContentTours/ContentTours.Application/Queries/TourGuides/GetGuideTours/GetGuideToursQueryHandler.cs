using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuides.GetGuideTours;

internal sealed class GetGuideToursQueryHandler(
    IGuideTourOfferingRepository offeringRepository,
    ITourRepository tourRepository,
    ILogger<GetGuideToursQueryHandler> logger)
    : IQueryHandler<GetGuideToursQuery, GetGuideToursResult>
{
    public async Task<Result<GetGuideToursResult>> Handle(
        GetGuideToursQuery request,
        CancellationToken cancellationToken)
    {
        var allOfferings = await offeringRepository
            .GetByGuideIdAsync(request.TourGuideId, cancellationToken)
            .ConfigureAwait(false);

        var totalCount = allOfferings.Count;

        var paged = allOfferings
            .OrderByDescending(o => o.AssignedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = new List<GuideTourListItemDto>(paged.Count);

        foreach (var offering in paged)
        {
            var tour = await tourRepository
                .GetByIdAsync(offering.TourId, cancellationToken)
                .ConfigureAwait(false);

            items.Add(new GuideTourListItemDto(
                TourId: offering.TourId,
                Title: tour?.Name ?? "Unknown Tour",
                Slug: tour?.Slug,
                IsProposer: offering.IsProposer,
                OfferingStatus: offering.Status.ToString(),
                OffersPrivateTour: offering.OffersPrivateTour,
                AssignedAt: offering.AssignedAt));
        }

        logger.LogDebug("Listed {Count}/{Total} tours for guide {GuideId}",
            items.Count, totalCount, request.TourGuideId);

        return Result.Success(new GetGuideToursResult(items, totalCount));
    }
}
