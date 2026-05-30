using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetMyBookings;

/// <summary>
/// Handler for <see cref="GetMyBookingsQuery"/>. Returns the caller's bookings, cursor-paginated.
/// </summary>
public sealed class GetMyBookingsQueryHandler(
    ITourBookingRepository repository,
    ILogger<GetMyBookingsQueryHandler> logger)
    : IQueryHandler<GetMyBookingsQuery, MyBookingsPage>
{
    public async Task<Result<MyBookingsPage>> Handle(GetMyBookingsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.UserId == Guid.Empty)
            {
                logger.LogWarning("GetMyBookings rejected: empty UserId.");
                return Result.Failure<MyBookingsPage>(
                    new Error("TourBooking.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            if (!MyBookingsCursor.TryDecode(request.Cursor, out var cursor))
            {
                logger.LogWarning(
                    "GetMyBookings rejected: malformed cursor for user {UserId}.",
                    request.UserId);
                return Result.Failure<MyBookingsPage>(
                    new Error("TourBooking.InvalidCursor", "The pagination cursor is malformed."),
                    Outcome.Invalid);
            }

            if (request.FromDate.HasValue && request.ToDate.HasValue
                && request.FromDate.Value > request.ToDate.Value)
            {
                return Result.Failure<MyBookingsPage>(
                    new Error("TourBooking.InvalidDateRange", "fromDate cannot be later than toDate."),
                    Outcome.Invalid);
            }

            var pageSize = request.EffectivePageSize;
            var limit = pageSize + 1; // fetch one extra to detect "has more".

            var rows = await repository.QueryUserBookingsAsync(
                request.UserId,
                request.Statuses,
                request.FromDate,
                request.ToDate,
                request.TourId,
                cursor?.CreatedAt,
                cursor?.Id,
                limit,
                cancellationToken).ConfigureAwait(false);

            string? nextCursor = null;
            IReadOnlyList<TourBooking> pageRows = rows;
            if (rows.Count > pageSize)
            {
                var trimmed = new TourBooking[pageSize];
                for (var i = 0; i < pageSize; i++)
                {
                    trimmed[i] = rows[i];
                }

                pageRows = trimmed;
                var last = trimmed[pageSize - 1];
                nextCursor = new MyBookingsCursor(last.Id, last.CreatedAt).Encode();
            }

            int? totalCount = null;
            if (request.CountTotal)
            {
                totalCount = await repository.CountUserBookingsAsync(
                    request.UserId,
                    request.Statuses,
                    request.FromDate,
                    request.ToDate,
                    request.TourId,
                    cancellationToken).ConfigureAwait(false);
            }

            var items = new MyBookingItemDto[pageRows.Count];
            for (var i = 0; i < pageRows.Count; i++)
            {
                items[i] = MapToDto(pageRows[i]);
            }

            logger.LogInformation(
                "GetMyBookings returned {ItemCount} item(s) for user {UserId} (hasNext: {HasNext}, total: {Total}).",
                items.Length,
                request.UserId,
                nextCursor is not null,
                totalCount);

            return Result.Success(new MyBookingsPage(items, nextCursor, totalCount));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<MyBookingsPage>(
                new Error("Request.Cancelled", "The request was cancelled before completion."),
                Outcome.Canceled);
        }
    }

    private static MyBookingItemDto MapToDto(TourBooking booking)
        => new(
            booking.Id,
            booking.Reference,
            booking.Status,
            booking.TourId,
            booking.ProviderId,
            booking.AvailabilitySlotId,
            booking.ParticipantCount,
            booking.TotalAmount,
            booking.Currency,
            booking.IsInstantBooking,
            booking.PaymentExpiresAt,
            booking.ConfirmedAt,
            booking.CancelledAt,
            booking.CompletedAt,
            booking.CreatedAt);
}
