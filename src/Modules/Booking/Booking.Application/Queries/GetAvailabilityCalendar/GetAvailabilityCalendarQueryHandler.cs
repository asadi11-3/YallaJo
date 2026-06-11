using Booking.Application.Commands.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetAvailabilityCalendar;

/// <summary>
/// [Backend] B3 — groups a tour's active slots by day for the requested month.
/// Same ownership gate as the manage list (provider owner or admin).
/// </summary>
public sealed class GetAvailabilityCalendarQueryHandler(
    IAvailabilitySlotRepository slotRepository,
    IBookingTourSnapshotReader tourSnapshotReader,
    IBookingProviderSnapshotReader providerSnapshotReader,
    ICurrentUser currentUser)
    : IQueryHandler<GetAvailabilityCalendarQuery, IReadOnlyList<AvailabilityCalendarDayDto>>
{
    public async Task<Result<IReadOnlyList<AvailabilityCalendarDayDto>>> Handle(
        GetAvailabilityCalendarQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Month is < 1 or > 12 || request.Year is < 2000 or > 2100)
        {
            return Result.Failure<IReadOnlyList<AvailabilityCalendarDayDto>>(
                new Error("AvailabilitySlot.InvalidMonth", "year/month must form a valid calendar month."),
                Outcome.Invalid);
        }

        // Owner/admin only — same gate the slot mutation commands use.
        var ownership = await AvailabilitySlotOwnership.ResolveAsync(
            request.TourId,
            currentUser,
            tourSnapshotReader,
            providerSnapshotReader,
            cancellationToken).ConfigureAwait(false);

        if (!ownership.IsSuccess)
        {
            return Result.Failure<IReadOnlyList<AvailabilityCalendarDayDto>>(
                ownership.Errors[0], ownership.Outcome);
        }

        var monthStart = new DateOnly(request.Year, request.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var slots = await slotRepository
            .GetByTourIdAsync(request.TourId, cancellationToken)
            .ConfigureAwait(false);

        var days = slots
            .Where(s => s.IsActive && s.Date >= monthStart && s.Date < monthEnd)
            .GroupBy(s => s.Date)
            .OrderBy(g => g.Key)
            .Select(g => new AvailabilityCalendarDayDto(
                Date: g.Key,
                SlotCount: g.Count(),
                TotalCapacity: g.Sum(s => s.MaxCapacity),
                BookedSeats: g.Sum(s => s.MaxCapacity - s.AvailableCount),
                HasOpenSlots: g.Any(s => s.AvailableCount > 0)))
            .ToList();

        return Result.Success<IReadOnlyList<AvailabilityCalendarDayDto>>(days);
    }
}
