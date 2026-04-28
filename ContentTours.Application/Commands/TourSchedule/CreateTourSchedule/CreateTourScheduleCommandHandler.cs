using ContentTours.Application.Caching;
using ContentTours.Application.Commands.TourSchedule.Common;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;

/// <summary>
/// Weekly-only model: creates one TourSchedule row per requested DayOfWeek (max 7).
/// Booking module instantiates real date/time slots at booking time.
/// Idempotent: skips (DayOfWeek, StartTime) pairs that already exist on this tour.
/// Returns { Created, Skipped } so caller sees what happened.
/// </summary>
public sealed class CreateTourScheduleCommandHandler(
    ITourRepository tourRepo,
    ITourScheduleRepository scheduleRepo,
    IContentToursUnitOfWork unitOfWork,
    IContentToursOutboxWriter outbox,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateTourScheduleCommandHandler> logger)
    : ICommandHandler<CreateTourScheduleCommand, CreateTourScheduleResult>
{
    public async Task<Result<CreateTourScheduleResult>> Handle(
        CreateTourScheduleCommand cmd, CancellationToken ct)
    {
        // 1. Load parent tour
        var tour = await tourRepo.GetByIdAsync(cmd.TourId, ct);
        if (tour is null || tour.IsDeleted)
            return Result.NotFound<CreateTourScheduleResult>("Tour.NotFound");

        if (tour.Status is TourStatus.Suspended or TourStatus.Archived)
            logger.LogWarning(
                "Creating schedule on {Status} tour {TourId} — allowed but logged",
                tour.Status, tour.Id);

        // 2. Owner-or-admin check
        if (tour.CreatedByUserId != currentUser.UserId!.Value && !currentUser.IsInRole("Admin"))
            return Result.Forbidden<CreateTourScheduleResult>("Tour.NotOwner");

        // 3. Deduplicate requested days (caller may send [1, 1, 3])
        var requestedDays = cmd.DaysOfWeek.Distinct().ToList();

        // 4. Load existing active schedules for this tour
        var existing = await scheduleRepo.GetAllAsync(
            filter: s => s.TourId == cmd.TourId && s.IsActive,
            ct: ct);

        // 5. Build candidates — one row per unique requested day
        var candidates = requestedDays
            .Select(dow => (DayOfWeek: dow, cmd.StartTime, cmd.EndTime))
            .ToList();

        // 6. Overlap validation: intervals are half-open [Start, End).
        //    EndTime==null treated as TimeOnly.MaxValue (open-ended for the day).
        var overlapError = TourScheduleOverlapChecker.Check(existing, candidates);
        if (overlapError is not null)
            return Result.UnprocessableEntity<CreateTourScheduleResult>(overlapError);

        // 7. Idempotency — skip only when (DayOfWeek, StartTime, EndTime) ALL match.
        //    If StartTime matches but EndTime differs → caller's update was silently lost.
        //    Return a clear 422 so they know to PUT the existing row instead.
        var existingByKey = existing
            .ToDictionary(s => (s.DayOfWeek, s.StartTime));

        int created = 0, skipped = 0;
        foreach (var (dow, start, end) in candidates)
        {
            if (existingByKey.TryGetValue((dow, start), out var existingRow))
            {
                if (existingRow.EndTime == end)
                {
                    // Exact match — true idempotent skip
                    skipped++;
                    continue;
                }

                // StartTime matches but EndTime differs — caller intended a change, not a duplicate
                return Result.UnprocessableEntity<CreateTourScheduleResult>(
                    new Error("TourSchedule.AlreadyExistsWithDifferentEndTime",
                        $"A schedule for DayOfWeek={dow} starting at {start} already exists " +
                        $"with EndTime={existingRow.EndTime?.ToString() ?? "open-ended"}. " +
                        $"Use PUT /schedules/{existingRow.Id} to update it."));
            }

            var schedule = ContentTours.Domain.Entities.TourSchedule.Create(cmd.TourId, dow, start, end, cmd.IsActive);
            await scheduleRepo.AddAsync(schedule, ct);

            outbox.Enqueue(new TourScheduleChangedIntegrationEvent(
                schedule.Id, cmd.TourId, dow, start, end, TourEntityChangeType.Created));

            created++;
        }

        if (created > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);

            await cache.RemoveByTagAsync(TourScheduleCacheKeys.TagForTour(cmd.TourId), ct);
            await cache.RemoveByTagAsync(TourCacheKeys.TagForTour(cmd.TourId), ct);

            logger.LogInformation(
                "Created {Created} TourSchedule rows for TourId={TourId} (skipped {Skipped})",
                created, cmd.TourId, skipped);
        }

        // 201 only when at least one row was actually created; 200 when all were idempotent skips
        return created > 0
            ? Result.Created(new CreateTourScheduleResult(created, skipped))
            : Result.Success(new CreateTourScheduleResult(0, skipped));
    }

}
