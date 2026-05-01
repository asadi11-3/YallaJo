using ContentTours.Application.Caching;
using ContentTours.Application.Commands.TourSchedule.Common;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourSchedule.UpdateTourSchedule;

public sealed class UpdateTourScheduleCommandHandler(
    ITourRepository tourRepo,
    ITourScheduleRepository scheduleRepo,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateTourScheduleCommandHandler> logger)
    : ICommandHandler<UpdateTourScheduleCommand>
{
    public async Task<Result> Handle(UpdateTourScheduleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepo.GetByIdAsync(request.TourId, cancellationToken);
            if (tour is null || tour.IsDeleted)
            {
                return Result.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                    Outcome.NotFound);
            }

            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result.Failure(
                    new Error("Tour.NotOwner", "You do not have permission to update schedules for this tour."),
                    Outcome.Forbidden);
            }

            var schedule = await scheduleRepo.GetByIdAsync(request.ScheduleId, cancellationToken);
            if (schedule is null || schedule.TourId != request.TourId)
            {
                return Result.Failure(
                   new Error("TourSchedule.NotFound", $"Schedule '{request.ScheduleId}' was not found on this tour."),
                   Outcome.NotFound);
            }

            // Overlap check: existing active rows minus this one + proposed change
            var existing = await scheduleRepo.GetAllAsync(
                filter: s => s.TourId == request.TourId && s.IsActive && s.Id != request.ScheduleId,
                ct: cancellationToken);

            var proposed = new[] { (request.DayOfWeek, request.StartTime, request.EndTime) };
            var overlapError = TourScheduleOverlapChecker.Check(existing, proposed);
            if (overlapError is not null)
                return Result.UnprocessableEntity(overlapError);

            schedule.Update(request.DayOfWeek, request.StartTime, request.EndTime, request.IsActive);

            outbox.Enqueue(new TourScheduleChangedIntegrationEvent(
                schedule.Id, request.TourId, request.DayOfWeek, request.StartTime, request.EndTime, TourEntityChangeType.Updated));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(
                    ex,
                    "Concurrency conflict while {Action}: ScheduleId={ScheduleId} TourId={TourId}",
                    nameof(UpdateTourScheduleCommand), request.ScheduleId, request.TourId);

                return Result.Failure(
                    new Error(
                        "TourSchedule.ConcurrencyConflict",
                        "This schedule was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourScheduleCacheKeys.TagForTour(request.TourId), cancellationToken);
            await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(request.TourId), cancellationToken);

            logger.LogInformation("Updated TourSchedule {ScheduleId} on TourId={TourId}", schedule.Id, request.TourId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
