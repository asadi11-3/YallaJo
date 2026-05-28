using Booking.Application.Caching;
using Booking.Application.Commands.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.CreateBulkAvailabilitySlots;

public sealed class CreateBulkAvailabilitySlotsCommandHandler(
    IAvailabilitySlotRepository availabilitySlotRepository,
    IBookingTourSnapshotReader tourSnapshotReader,
    IBookingProviderSnapshotReader providerSnapshotReader,
    IBookingUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateBulkAvailabilitySlotsCommandHandler> logger)
    : ICommandHandler<CreateBulkAvailabilitySlotsCommand, CreateBulkAvailabilitySlotsResult>
{
    private const int MaxBulkWindowDays = 90;

    public async Task<Result<CreateBulkAvailabilitySlotsResult>> Handle(
        CreateBulkAvailabilitySlotsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ownership = await AvailabilitySlotOwnership.ResolveAsync(
                request.TourId,
                currentUser,
                tourSnapshotReader,
                providerSnapshotReader,
                cancellationToken).ConfigureAwait(false);

            if (!ownership.IsSuccess)
            {
                return Result.Failure<CreateBulkAvailabilitySlotsResult>(ownership.Errors[0], ownership.Outcome);
            }

            var ownerContext = ownership.Value!;
            if (request.MaxCapacity > ownerContext.TourSnapshot.MaxGroupSize)
            {
                return Result.Failure<CreateBulkAvailabilitySlotsResult>(
                    new Error(
                        "AvailabilitySlot.CapacityExceedsTour",
                        $"MaxCapacity cannot exceed the tour group limit ({ownerContext.TourSnapshot.MaxGroupSize})."),
                    Outcome.Invalid);
            }

            var clampedEndDate = request.EndDate > request.StartDate.AddDays(MaxBulkWindowDays)
                ? request.StartDate.AddDays(MaxBulkWindowDays)
                : request.EndDate;

            var candidateDates = AvailabilitySlotRecurrenceExpander.Expand(
                request.StartDate,
                clampedEndDate,
                request.Recurrence,
                request.DaysOfWeek);

            if (candidateDates.Count == 0)
            {
                return Result.Failure<CreateBulkAvailabilitySlotsResult>(
                    new Error("AvailabilitySlot.NothingToCreate", "No candidate dates were produced by the recurrence rule."),
                    Outcome.UnprocessableEntity);
            }

            var tourGuideId = await availabilitySlotRepository
                .GetTourGuideIdByUserIdAsync(ownerContext.ProviderSnapshot.OwnerUserId, cancellationToken)
                .ConfigureAwait(false);

            if (tourGuideId is null)
            {
                return Result.Failure<CreateBulkAvailabilitySlotsResult>(
                    new Error(
                        AvailabilitySlotOwnership.NotAProviderErrorCode,
                        "Current user is not registered as an active tour guide."),
                    Outcome.Forbidden);
            }

            var existing = await availabilitySlotRepository
                .GetActiveSlotsForTourInRangeAsync(request.TourId, request.StartDate, clampedEndDate, cancellationToken)
                .ConfigureAwait(false);

            var existingByDate = existing
                .GroupBy(s => s.Date)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<AvailabilitySlot>)g.ToList());

            var skippedDates = new HashSet<DateOnly>();
            var created = new List<AvailabilitySlot>();

            foreach (var date in candidateDates)
            {
                var hasExistingOnDate = existingByDate.TryGetValue(date, out var slotsOnDate);
                if (!hasExistingOnDate || slotsOnDate is null)
                {
                    created.Add(AvailabilitySlot.CreateForTour(
                        tourGuideId.Value,
                        request.TourId,
                        date,
                        request.StartTime,
                        request.EndTime,
                        request.MaxCapacity));
                    continue;
                }

                var identical = slotsOnDate.Any(s =>
                    s.StartTime == request.StartTime
                    && s.EndTime == request.EndTime
                    && s.IsActive);

                var overlaps = slotsOnDate.Any(s =>
                    !(s.EndTime <= request.StartTime || s.StartTime >= request.EndTime));

                if (identical && request.SkipExisting)
                {
                    skippedDates.Add(date);
                    continue;
                }

                if ((identical || overlaps) && !request.SkipExisting)
                {
                    return Result.Failure<CreateBulkAvailabilitySlotsResult>(
                        new Error(
                            "AvailabilitySlot.Overlap",
                            $"Slot collision detected on {date:yyyy-MM-dd}."),
                        Outcome.Conflict);
                }

                if (overlaps)
                {
                    return Result.Failure<CreateBulkAvailabilitySlotsResult>(
                        new Error(
                            "AvailabilitySlot.Overlap",
                            $"Overlapping slot detected on {date:yyyy-MM-dd}."),
                        Outcome.Conflict);
                }

                created.Add(AvailabilitySlot.CreateForTour(
                    tourGuideId.Value,
                    request.TourId,
                    date,
                    request.StartTime,
                    request.EndTime,
                    request.MaxCapacity));
            }

            if (created.Count == 0)
            {
                return Result.Failure<CreateBulkAvailabilitySlotsResult>(
                    new Error(
                        "AvailabilitySlot.NothingToCreate",
                        "All candidate dates were skipped because matching slots already exist."),
                    Outcome.UnprocessableEntity);
            }

            await availabilitySlotRepository.AddRangeAsync(created, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<CreateBulkAvailabilitySlotsResult>(
                    new Error("AvailabilitySlot.CapacityConflict", "Slot was modified by another booking."),
                    Outcome.Conflict);
            }
            catch (DbUpdateException)
            {
                return Result.Failure<CreateBulkAvailabilitySlotsResult>(
                    new Error("AvailabilitySlot.BulkPersistFailed", "Bulk create failed due to a persistence constraint."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(BookingAvailabilityCacheKeys.TourTag(request.TourId), cancellationToken)
                .ConfigureAwait(false);
            foreach (var affectedDate in created.Select(s => s.Date).Distinct())
            {
                await cache.RemoveByTagAsync(
                        BookingAvailabilityCacheKeys.TourDateTag(request.TourId, affectedDate),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "Availability slot {SlotId} created by {UserId}",
                created[0].Id,
                currentUser.UserId);

            return Result.Success(new CreateBulkAvailabilitySlotsResult(
                CreatedCount: created.Count,
                SkippedDates: skippedDates.OrderBy(d => d).ToList(),
                TotalRequested: candidateDates.Count));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<CreateBulkAvailabilitySlotsResult>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
