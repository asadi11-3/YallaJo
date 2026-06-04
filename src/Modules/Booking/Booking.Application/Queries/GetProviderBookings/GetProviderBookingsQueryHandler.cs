using Booking.Application.Interfaces;
using Booking.Application.Queries.GetAllBookings;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetProviderBookings;

public sealed class GetProviderBookingsQueryHandler(
    ITourBookingRepository repository,
    IAvailabilitySlotRepository slotRepository,
    IBookingProviderSnapshotReader providerSnapshotReader,
    ILogger<GetProviderBookingsQueryHandler> logger)
    : IQueryHandler<GetProviderBookingsQuery, ProviderBookingsPage>
{
    public async Task<Result<ProviderBookingsPage>> Handle(
        GetProviderBookingsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Reuse the admin cursor format (same (CreatedAt, Id) shape).
            if (!AdminBookingsCursor.TryDecode(request.Cursor, out var cursor))
            {
                return Result.Failure<ProviderBookingsPage>(
                    new Error("TourBooking.InvalidCursor", "The supplied cursor is malformed."),
                    Outcome.Invalid);
            }

            if (request.FromDate is { } from && request.ToDate is { } to && from > to)
            {
                return Result.Failure<ProviderBookingsPage>(
                    new Error("TourBooking.InvalidDateRange", "fromDate must be on or before toDate."),
                    Outcome.Invalid);
            }

            // Resolve the caller's provider. No provider → no bookings (empty page),
            // never another provider's data.
            var provider = await providerSnapshotReader
                .GetByOwnerUserIdAsync(request.CallerUserId, cancellationToken)
                .ConfigureAwait(false);

            if (provider is null)
            {
                logger.LogInformation(
                    "GetProviderBookings: caller {UserId} owns no provider; returning empty page.",
                    request.CallerUserId);
                return Result.Success(new ProviderBookingsPage([], null, request.CountTotal ? 0 : null));
            }

            var pageSize = request.EffectivePageSize;

            var rows = await repository.QueryAllBookingsAsync(
                statuses: request.Statuses,
                fromDate: request.FromDate,
                toDate: request.ToDate,
                userId: null,
                providerId: provider.ProviderId,
                tourId: request.TourId,
                paymentStatus: null,
                cursorCreatedAt: cursor?.CreatedAt,
                cursorId: cursor?.Id,
                limit: pageSize + 1,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            string? nextCursor = null;
            var page = rows;

            if (rows.Count > pageSize)
            {
                page = [.. rows.Take(pageSize)];
                var last = page[^1];
                nextCursor = new AdminBookingsCursor(last.Id, last.CreatedAt).Encode();
            }

            // Bulk-load slot times for the page (one query, no N+1).
            var slotIds = page.Select(b => b.AvailabilitySlotId).Distinct().ToList();
            var slots = await slotRepository.GetByIdsAsync(slotIds, cancellationToken).ConfigureAwait(false);
            var slotsById = slots.ToDictionary(s => s.Id);

            int? totalCount = null;
            if (request.CountTotal)
            {
                totalCount = await repository.CountAllBookingsAsync(
                    statuses: request.Statuses,
                    fromDate: request.FromDate,
                    toDate: request.ToDate,
                    userId: null,
                    providerId: provider.ProviderId,
                    tourId: request.TourId,
                    paymentStatus: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            var items = page.Select(b => MapToDto(b, slotsById)).ToList();
            return Result.Success(new ProviderBookingsPage(items, nextCursor, totalCount));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<ProviderBookingsPage>(
                new Error("Request.Cancelled", "Request was canceled."),
                Outcome.Canceled);
        }
    }

    private static ProviderBookingItemDto MapToDto(
        TourBooking booking, IReadOnlyDictionary<Guid, AvailabilitySlot> slotsById)
    {
        slotsById.TryGetValue(booking.AvailabilitySlotId, out var slot);

        return new ProviderBookingItemDto(
            Id: booking.Id,
            Reference: booking.Reference,
            Status: booking.Status,
            UserId: booking.UserId,
            TourId: booking.TourId,
            ProviderId: booking.ProviderId,
            AvailabilitySlotId: booking.AvailabilitySlotId,
            SlotDate: slot?.Date,
            SlotStartTime: slot?.StartTime,
            SlotEndTime: slot?.EndTime,
            ParticipantCount: booking.ParticipantCount,
            TotalAmount: booking.TotalAmount,
            Currency: booking.Currency,
            IsInstantBooking: booking.IsInstantBooking,
            PaymentExpiresAt: booking.PaymentExpiresAt,
            ConfirmedAt: booking.ConfirmedAt,
            CancelledAt: booking.CancelledAt,
            CompletedAt: booking.CompletedAt,
            CreatedAt: booking.CreatedAt);
    }
}
