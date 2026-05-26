using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.GuideTourOffering.GetGuideOfferings;

internal sealed class GetGuideOfferingsQueryHandler(
    IGuideTourOfferingRepository offeringRepository,
    ILogger<GetGuideOfferingsQueryHandler> logger) : IQueryHandler<GetGuideOfferingsQuery, IReadOnlyList<GuideOfferingDto>>
{
    public async Task<Result<IReadOnlyList<GuideOfferingDto>>> Handle(GetGuideOfferingsQuery request, CancellationToken cancellationToken)
    {
        var offerings = await offeringRepository.GetByTourIdAsync(request.TourId, cancellationToken);

        var dtos = offerings.Select(o => new GuideOfferingDto(
            o.Id, o.TourId, o.TourGuideId, o.Status.ToString(),
            o.OffersPrivateTour, o.PrivateTourPriceMultiplier, o.PrivateTourFlatPrice,
            o.IsProposer, o.AssignedAt, o.CreatedAt)).ToList();

        logger.LogInformation("Retrieved {Count} guide offerings for TourId={TourId}", dtos.Count, request.TourId);
        return Result.Success<IReadOnlyList<GuideOfferingDto>>(dtos);
    }
}
