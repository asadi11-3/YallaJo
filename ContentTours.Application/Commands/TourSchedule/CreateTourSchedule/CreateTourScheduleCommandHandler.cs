using ContentTours.Application.Caching;
using ContentTours.Application.Commands.TourSchedule.Common;
using ContentTours.Application.Interfaces;
using ContentTours.Contracts;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;

/// <summary>
/// Recurrence-pattern expansion handler — PDF Task-2A B2.
///
/// Pipeline:
///   1. Load parent tour + ownership/admin gate (Suspended/Archived → write allowed but logged Warning).
///   2. <see cref="TourScheduleRecurrenceExpander"/> projects the pattern descriptor into 1..N candidate
///      <c>(DayOfWeek, StartTime, EndTime)</c> tuples under the 90-day cap and 120-row hard limit.
///   3. Overlap validation (<see cref="TourScheduleOverlapChecker"/>) on existing-active ∪ proposed.
///   4. Idempotency per PDF: skip when <c>(TourId, DayOfWeek, StartTime)</c> match an existing row.
///      Emit a <c>Warning</c> log when EndTime differs (operator visibility for PUT-instead-of-POST drift).
///   5. Outbox <see cref="TourScheduleChangedIntegrationEvent"/> per newly-created row, BEFORE SaveChanges.
///   6. Cache invalidation (<c>tour-schedules:{tourId}</c> + <c>tour:{tourId}</c>) after a successful save.
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
        CreateTourScheduleCommand request, CancellationToken cancellationToken)
    {
        try
        {
        // 1. Parent tour load + ownership gate
        var tour = await tourRepo.GetByIdAsync(request.TourId, cancellationToken);
        if (tour is null || tour.IsDeleted)
            {
              return Result<CreateTourScheduleResult>.Failure(
                            new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                            Outcome.NotFound);
            }

        if (tour.Status is TourStatus.Suspended or TourStatus.Archived)
            {
                logger.LogWarning(
                "Creating schedule on {Status} tour {TourId} - allowed but logged",
                tour.Status, tour.Id);
            }

        var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
            >= RolePrivilegeLevel.Admin;
        if (!isAdminTier && tour.CreatedByUserId != currentUser.UserId!.Value)
            {
                return Result<CreateTourScheduleResult>.Failure(
                new Error("Tour.NotOwner", "You do not have permission to create schedules for this tour."),
                Outcome.Forbidden);
            }

        // 2. Pattern expansion
        var expansion = TourScheduleRecurrenceExpander.Expand(
            request.Pattern,
            request.DaysOfWeek,
            request.CustomDates,
            request.StartTime,
            request.EndTime,
            request.ValidFrom,
            request.ValidTo);

        if (!expansion.IsSuccess)
            return Result<CreateTourScheduleResult>.Failure(expansion.Errors[0], expansion.Outcome);

        var candidates = expansion.Value!;

        if (candidates.Count == 0)
        {
            // Empty expansion (e.g. Weekly with no matching days inside the cap window):
            // treat as a no-op success rather than a 422.
            logger.LogInformation(
                "Recurrence expansion for TourId={TourId} produced 0 candidates (pattern={Pattern})",
                request.TourId, request.Pattern);
            return Result.Success(new CreateTourScheduleResult(0, 0));
        }

        // 3. Load existing ACTIVE rows once. The (DayOfWeek, StartTime) keyed view is reused
        //    by the overlap-exclusion filter (so candidates that idempotently skip do not
        //    falsely trip overlap on themselves) AND by the idempotency loop below.
        var existing = await scheduleRepo.GetAllAsync(
            filter: s => s.TourId == request.TourId && s.IsActive,
            ct: cancellationToken);

        // 4. Idempotency per PDF Task-2A B2: skip on (TourId, DayOfWeek, StartTime) match.
        //    EndTime divergence is NOT an error (per chosen design) but is logged at Warning
        //    so an operator can spot a caller using POST when they should be using PUT.
        var existingByKey = existing
            .GroupBy(s => (s.DayOfWeek, s.StartTime))
            .ToDictionary(g => g.Key, g => g.First());

        // 5. Overlap validation: only on candidates that would actually be inserted.
        //    Candidates whose (DayOfWeek, StartTime) already exist will idempotently skip
        //    in step 6 below — feeding them into the overlap checker would cause the
        //    proposed row to "overlap" with the existing row it is about to no-op against
        //    (a Petra 9:00–11:00 candidate would conflict with the very 9:00–11:00 row it
        //    is meant to deduplicate against).
        var proposedForOverlap = candidates
            .Where(c => !existingByKey.ContainsKey((c.DayOfWeek, c.StartTime)))
            .Select(c => (c.DayOfWeek, c.StartTime, c.EndTime))
            .Distinct()
            .ToList();

        var overlapError = TourScheduleOverlapChecker.Check(existing, proposedForOverlap);
        if (overlapError is not null)
            return Result<CreateTourScheduleResult>.Failure(overlapError, Outcome.UnprocessableEntity);

        // Within a single expansion an emitted (DayOfWeek, StartTime) may repeat across dates
        // (Daily / Weekly / Custom-with-duplicates). Track keys we've already inserted in
        // THIS call so we don't try to insert the same weekly row twice.
        var insertedKeys = new HashSet<(byte DayOfWeek, TimeOnly StartTime)>();

        int created = 0, skipped = 0;
        foreach (var candidate in candidates)
        {
            var key = (candidate.DayOfWeek, candidate.StartTime);

            if (insertedKeys.Contains(key))
            {
                // Same weekly slot already inserted earlier in this expansion — idempotent skip.
                skipped++;
                continue;
            }

            if (existingByKey.TryGetValue(key, out var existingRow))
            {
                if (existingRow.EndTime != candidate.EndTime)
                {
                    logger.LogWarning(
                        "TourSchedule idempotency skip with EndTime drift: TourId={TourId} " +
                        "DayOfWeek={DayOfWeek} StartTime={StartTime} " +
                        "ExistingEndTime={ExistingEndTime} RequestedEndTime={RequestedEndTime}. " +
                        "Use PUT to update the existing row's EndTime.",
                        request.TourId, candidate.DayOfWeek, candidate.StartTime,
                        existingRow.EndTime, candidate.EndTime);
                }

                skipped++;
                continue;
            }

            var schedule = ContentTours.Domain.Entities.TourSchedule.Create(
                request.TourId, candidate.DayOfWeek, candidate.StartTime, candidate.EndTime, request.IsActive);
            await scheduleRepo.AddAsync(schedule, cancellationToken);

            outbox.Enqueue(new TourScheduleChangedIntegrationEvent(
                schedule.Id, request.TourId,
                candidate.DayOfWeek, candidate.StartTime, candidate.EndTime,
                TourEntityChangeType.Created));

            insertedKeys.Add(key);
            created++;
        }

        if (created > 0)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(
                    ex,
                    "Concurrency conflict while {Action}: TourId={TourId}",
                    nameof(CreateTourScheduleCommand), request.TourId);

                return Result<CreateTourScheduleResult>.Failure(
                    new Error(
                        "TourSchedule.ConcurrencyConflict",
                        "This schedule was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(TourScheduleCacheKeys.TagForTour(request.TourId), cancellationToken);
            await cache.RemoveByTagAsync(ContentToursCacheKeys.TagForTour(request.TourId), cancellationToken);

            logger.LogInformation(
                "Created {Created} TourSchedule rows for TourId={TourId} (pattern={Pattern}, skipped={Skipped})",
                created, request.TourId, request.Pattern, skipped);
        }

        // 201 only when at least one row was actually created; 200 when all were idempotent skips.
        return created > 0
            ? Result.Created(new CreateTourScheduleResult(created, skipped))
            : Result.Success(new CreateTourScheduleResult(0, skipped));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreateTourScheduleResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
