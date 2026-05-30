using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.GuideTourOffering.GetGuideSchedules;

internal sealed class GetGuideSchedulesQueryHandler(
    IGuideScheduleRepository scheduleRepository,
    ILogger<GetGuideSchedulesQueryHandler> logger) : IQueryHandler<GetGuideSchedulesQuery, IReadOnlyList<GuideScheduleDto>>
{
    public async Task<Result<IReadOnlyList<GuideScheduleDto>>> Handle(GetGuideSchedulesQuery request, CancellationToken cancellationToken)
    {
        var schedules = await scheduleRepository.GetByTourAndGuideAsync(request.TourId, request.TourGuideId, cancellationToken);

        var dtos = schedules.Select(s => new GuideScheduleDto(
            s.Id, s.DayOfWeek, s.StartTime, s.EndTime, s.IsActive, s.CreatedAt)).ToList();

        logger.LogInformation("Retrieved {Count} schedules for TourId={TourId}, GuideId={GuideId}", dtos.Count, request.TourId, request.TourGuideId);
        return Result.Success<IReadOnlyList<GuideScheduleDto>>(dtos);
    }
}
