using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.ChildrenInfo.GetTourChildrenInfo;

public sealed class GetTourChildrenInfoQueryHandler(
    ITourRepository tourRepository,
    ILogger<GetTourChildrenInfoQueryHandler> logger)
    : IQueryHandler<GetTourChildrenInfoQuery, ChildrenInfoDto>
{
    public async Task<Result<ChildrenInfoDto>> Handle(
        GetTourChildrenInfoQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepository.GetAsync(
                    filter:       t => t.Id == request.TourId,
                    include:      q => q.Include(t => t.ChildFacilities),
                    asNoTracking: true,
                    ct:           cancellationToken)
                .ConfigureAwait(false);

            if (tour is null || tour.IsDeleted)
            {
                return Result<ChildrenInfoDto>.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                    Outcome.NotFound);
            }

            var facilityNames = tour.ChildFacilities
                .Select(cf => cf.Facility)
                .OrderBy(f => f)
                .Select(f => f.ToString())
                .ToList()
                as IReadOnlyList<string>;

            var dto = new ChildrenInfoDto(
                AllowsChildren:  tour.AllowsChildren,
                MinChildAge:     tour.MinChildAge,
                MaxChildAge:     tour.MaxChildAge,
                ChildFacilities: facilityNames);

            logger.LogDebug(
                "Loaded ChildrenInfo for TourId={TourId} (AllowsChildren={AllowsChildren}, FacilityCount={FacilityCount})",
                request.TourId,
                dto.AllowsChildren,
                dto.ChildFacilities.Count);

            return Result<ChildrenInfoDto>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<ChildrenInfoDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
