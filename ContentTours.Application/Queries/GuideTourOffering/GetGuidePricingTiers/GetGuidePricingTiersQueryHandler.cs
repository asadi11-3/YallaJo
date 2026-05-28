using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.GuideTourOffering.GetGuidePricingTiers;

internal sealed class GetGuidePricingTiersQueryHandler(
    IGuidePricingTierRepository pricingTierRepository,
    ILogger<GetGuidePricingTiersQueryHandler> logger) : IQueryHandler<GetGuidePricingTiersQuery, IReadOnlyList<GuidePricingTierDto>>
{
    public async Task<Result<IReadOnlyList<GuidePricingTierDto>>> Handle(GetGuidePricingTiersQuery request, CancellationToken cancellationToken)
    {
        var tiers = await pricingTierRepository.GetByTourAndGuideAsync(request.TourId, request.TourGuideId, cancellationToken);

        var dtos = tiers.Select(t => new GuidePricingTierDto(
            t.Id, t.Name, t.Description, t.Price.Amount, t.Price.Currency,
            t.MinParticipants, t.MaxParticipants, t.IsActive, t.CreatedAt)).ToList();

        logger.LogInformation("Retrieved {Count} pricing tiers for TourId={TourId}, GuideId={GuideId}", dtos.Count, request.TourId, request.TourGuideId);
        return Result.Success<IReadOnlyList<GuidePricingTierDto>>(dtos);
    }
}
