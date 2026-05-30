using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetAllBookings;

/// <summary>
/// Handler for <see cref="GetAllBookingsQuery"/>.
/// Authorization is enforced at the endpoint via <c>MustHavePermission(AdminBookingDashboard, Read)</c>;
/// the handler assumes the caller is already verified admin and applies NO self-scoping.
/// </summary>
public sealed class GetAllBookingsQueryHandler(
    ITourBookingRepository repository,
    ILogger<GetAllBookingsQueryHandler> logger)
    : IQueryHandler<GetAllBookingsQuery, AdminBookingsPage>
{
    public async Task<Result<AdminBookingsPage>> Handle(GetAllBookingsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!AdminBookingsCursor.TryDecode(request.Cursor, out var cursor))
            {
                logger.LogWarning("Admin bookings list rejected: malformed cursor.");
                return Result.Failure<AdminBookingsPage>(
                    new Error("TourBooking.InvalidCursor", "The supplied cursor is malformed."),
                    Outcome.Invalid);
            }

            if (request.FromDate is { } from && request.ToDate is { } to && from > to)
            {
                return Result.Failure<AdminBookingsPage>(
                    new Error("TourBooking.InvalidDateRange", "fromDate must be on or before toDate."),
                    Outcome.Invalid);
            }

            var pageSize = request.EffectivePageSize;

            // PaymentStatus filter is a stub for now (Finance owns the payment projection).
            // We forward the value to the repository which logs + ignores until Finance
            // ships a cross-module read model. Admins see ALL bookings regardless.
            if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                logger.LogInformation(
                    "Admin bookings list received paymentStatus filter '{PaymentStatus}'. " +
                    "Cross-module Finance projection not yet wired; filter is currently a no-op.",
                    request.PaymentStatus);
            }

            var rows = await repository.QueryAllBookingsAsync(
                statuses: request.Statuses,
                fromDate: request.FromDate,
                toDate: request.ToDate,
                userId: request.UserId,
                providerId: request.ProviderId,
                tourId: request.TourId,
                paymentStatus: request.PaymentStatus,
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

            int? totalCount = null;
            if (request.CountTotal)
            {
                totalCount = await repository.CountAllBookingsAsync(
                    statuses: request.Statuses,
                    fromDate: request.FromDate,
                    toDate: request.ToDate,
                    userId: request.UserId,
                    providerId: request.ProviderId,
                    tourId: request.TourId,
                    paymentStatus: request.PaymentStatus,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            var items = page.Select(MapToDto).ToList();
            return Result.Success(new AdminBookingsPage(items, nextCursor, totalCount));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<AdminBookingsPage>(
                new Error("Request.Cancelled", "Request was canceled."),
                Outcome.Canceled);
        }
    }

    private static AdminBookingItemDto MapToDto(TourBooking booking) => new(
        Id: booking.Id,
        Reference: booking.Reference,
        Status: booking.Status,
        UserId: booking.UserId,
        TourId: booking.TourId,
        ProviderId: booking.ProviderId,
        AvailabilitySlotId: booking.AvailabilitySlotId,
        ParticipantCount: booking.ParticipantCount,
        TotalAmount: booking.TotalAmount,
        Currency: booking.Currency,
        IsInstantBooking: booking.IsInstantBooking,
        PaymentExpiresAt: booking.PaymentExpiresAt,
        ConfirmedAt: booking.ConfirmedAt,
        RejectedAt: booking.RejectedAt,
        CancelledAt: booking.CancelledAt,
        CompletedAt: booking.CompletedAt,
        RefundAmount: booking.RefundAmount,
        CreatedAt: booking.CreatedAt);
}
