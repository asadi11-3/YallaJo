using ContentTours.Application.Queries.GuideTourOffering.GetGuidePricingTiers;
using ContentTours.Application.Queries.GuideTourOffering.GetGuideSchedules;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.GuideTourOffering.GetGuideOfferingDetail;

internal sealed class GetGuideOfferingDetailQueryHandler(
    IGuideTourOfferingRepository offeringRepository,
    IGuideScheduleRepository scheduleRepository,
    IGuidePricingTierRepository pricingTierRepository,
    ILogger<GetGuideOfferingDetailQueryHandler> logger) : IQueryHandler<GetGuideOfferingDetailQuery, GuideOfferingDetailDto>
{
    public async Task<Result<GuideOfferingDetailDto>> Handle(GetGuideOfferingDetailQuery request, CancellationToken cancellationToken)
    {
        var offering = await offeringRepository.GetByTourAndGuideAsync(request.TourId, request.TourGuideId, cancellationToken);
        if (offering is null)
            return Result<GuideOfferingDetailDto>.Failure(new Error("GuideTourOffering.NotFound", "Guide offering not found."), Outcome.NotFound);

        var schedules = await scheduleRepository.GetByOfferingIdAsync(offering.Id, cancellationToken);
        var tiers = await pricingTierRepository.GetByOfferingIdAsync(offering.Id, cancellationToken);

        var scheduleDtos = schedules.Select(s => new GuideScheduleDto(
            s.Id, s.DayOfWeek, s.StartTime, s.EndTime, s.IsActive, s.CreatedAt)).ToList();

        var tierDtos = tiers.Select(t => new GuidePricingTierDto(
            t.Id, t.Name, t.Description, t.Price.Amount, t.Price.Currency,
            t.MinParticipants, t.MaxParticipants, t.IsActive, t.CreatedAt)).ToList();

        var dto = new GuideOfferingDetailDto(
            offering.Id, offering.TourId, offering.TourGuideId, offering.Status.ToString(),
            offering.OffersPrivateTour, offering.PrivateTourPriceMultiplier, offering.PrivateTourFlatPrice,
            offering.IsProposer, offering.AssignedAt, offering.CreatedAt,
            scheduleDtos, tierDtos);

        logger.LogInformation("Retrieved offering detail TourId={TourId}, GuideId={GuideId}", request.TourId, request.TourGuideId);
        return Result.Success(dto);
    }
}
