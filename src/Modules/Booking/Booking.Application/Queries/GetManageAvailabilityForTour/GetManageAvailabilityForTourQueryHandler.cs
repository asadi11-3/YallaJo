using Booking.Application.Commands.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetManageAvailabilityForTour;

public sealed class GetManageAvailabilityForTourQueryHandler(
    IAvailabilitySlotRepository slotRepository,
    IBookingTourSnapshotReader tourSnapshotReader,
    IBookingProviderSnapshotReader providerSnapshotReader,
    ICurrentUser currentUser)
    : IQueryHandler<GetManageAvailabilityForTourQuery, IReadOnlyList<ManageAvailabilitySlotDto>>
{
    public async Task<Result<IReadOnlyList<ManageAvailabilitySlotDto>>> Handle(
        GetManageAvailabilityForTourQuery request,
        CancellationToken cancellationToken)
    {
        // Owner/admin only — same gate the slot mutation commands use.
        var ownership = await AvailabilitySlotOwnership.ResolveAsync(
            request.TourId,
            currentUser,
            tourSnapshotReader,
            providerSnapshotReader,
            cancellationToken).ConfigureAwait(false);

        if (!ownership.IsSuccess)
        {
            return Result.Failure<IReadOnlyList<ManageAvailabilitySlotDto>>(
                ownership.Errors[0], ownership.Outcome);
        }

        var slots = await slotRepository
            .GetByTourIdAsync(request.TourId, cancellationToken)
            .ConfigureAwait(false);

        var dtos = slots
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => new ManageAvailabilitySlotDto(
                Id: s.Id,
                TourId: s.TourId ?? request.TourId,
                TourGuideId: s.TourGuideId,
                Date: s.Date,
                StartTime: s.StartTime,
                EndTime: s.EndTime,
                MaxCapacity: s.MaxCapacity,
                AvailableCount: s.AvailableCount,
                IsActive: s.IsActive,
                RowVersion: Convert.ToBase64String(s.RowVersion ?? [])))
            .ToList();

        return Result.Success<IReadOnlyList<ManageAvailabilitySlotDto>>(dtos);
    }
}
