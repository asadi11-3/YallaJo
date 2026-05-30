using ContentTours.Application.Queries.TourSchedule.Common;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourSchedule.ListTourSchedules;

public sealed class ListTourSchedulesQueryHandler(
    ITourRepository tourRepo,
    ITourScheduleRepository scheduleRepo,
    ILogger<ListTourSchedulesQueryHandler> logger)
    : IQueryHandler<ListTourSchedulesQuery, IReadOnlyList<TourScheduleDto>>
{
    public async Task<Result<IReadOnlyList<TourScheduleDto>>> Handle(
        ListTourSchedulesQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepo.GetByIdAsync(query.TourId, cancellationToken);
            if (tour is null || tour.IsDeleted)
            {
                return Result<IReadOnlyList<TourScheduleDto>>.Failure(
                   new Error("Tour.NotFound", $"Tour '{query.TourId}' was not found."),
                   Outcome.NotFound);
            }

            var schedules = await scheduleRepo.GetAllAsync(
                filter: s => s.TourId == query.TourId && (!query.ActiveOnly || s.IsActive),
                orderBy: q => q.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime),
                ct: cancellationToken);

            var dtos = schedules
                .Select(s => new TourScheduleDto(
                    s.Id, s.TourId, s.DayOfWeek, s.StartTime, s.EndTime, s.IsActive, s.CreatedAt))
                .ToList();

            logger.LogDebug("Listed {Count} schedules for TourId={TourId}", dtos.Count, query.TourId);

            return Result.Success<IReadOnlyList<TourScheduleDto>>(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<TourScheduleDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
