using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetAvailabilityForTour;

public sealed class GetAvailabilityForTourQueryHandler(
    IAvailabilitySlotRepository availabilitySlotRepository,
    IBookingTourSnapshotReader tourSnapshotReader,
    ILogger<GetAvailabilityForTourQueryHandler> logger)
    : IQueryHandler<GetAvailabilityForTourQuery, AvailabilityPage>
{
    public async Task<Result<AvailabilityPage>> Handle(GetAvailabilityForTourQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!AvailabilityCursor.TryDecode(request.Cursor, out var cursor))
            {
                return Result.Failure<AvailabilityPage>(
                    new Error("AvailabilitySlot.InvalidCursor", "The pagination cursor is malformed."),
                    Outcome.Invalid);
            }

            var tour = await tourSnapshotReader.GetByIdAsync(request.TourId, cancellationToken).ConfigureAwait(false);
            if (tour is null || !tour.IsActive)
            {
                return Result.Failure<AvailabilityPage>(
                    new Error("Tour.NotFound", "Tour is unavailable."),
                    Outcome.NotFound);
            }

            var pageSize = request.EffectivePageSize;
            var rows = await availabilitySlotRepository
                .GetActiveSlotsForTourAsync(
                    request.TourId,
                    fromDate: DateOnly.FromDateTime(DateTime.UtcNow),
                    cursorDate: cursor?.Date,
                    cursorId: cursor?.Id,
                    limit: pageSize + 1,
                    ct: cancellationToken)
                .ConfigureAwait(false);

            string? nextCursor = null;
            IReadOnlyList<AvailabilitySlot> pageRows = rows;
            if (rows.Count > pageSize)
            {
                var trimmed = rows.Take(pageSize).ToList();
                pageRows = trimmed;
                var last = trimmed[^1];
                nextCursor = new AvailabilityCursor(last.Id, last.Date).Encode();
            }

            var grouped = pageRows
                .GroupBy(s => s.Date)
                .OrderBy(g => g.Key)
                .Select(g => new AvailabilityDateGroupDto(
                    Date: g.Key,
                    Slots: g.OrderBy(x => x.StartTime)
                        .ThenBy(x => x.Id)
                        .Select(MapSlot)
                        .ToList()))
                .ToList();

            int? totalCount = null;
            if (request.CountTotal)
            {
                totalCount = await availabilitySlotRepository
                    .CountActiveSlotsForTourAsync(request.TourId, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "Availability list for tour {TourId}: {Count} slot(s)",
                request.TourId,
                pageRows.Count);

            return Result.Success(new AvailabilityPage(grouped, nextCursor, totalCount));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<AvailabilityPage>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }

    private static AvailabilitySlotListItemDto MapSlot(AvailabilitySlot slot)
        => new(
            Id: slot.Id,
            StartTime: slot.StartTime,
            EndTime: slot.EndTime,
            AvailableCount: slot.AvailableCount);
}
