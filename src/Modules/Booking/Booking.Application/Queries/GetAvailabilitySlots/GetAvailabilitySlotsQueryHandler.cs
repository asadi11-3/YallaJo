using Booking.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetAvailabilitySlots;

public sealed class GetAvailabilitySlotsQueryHandler(
    IAvailabilitySlotRepository slotRepository)
    : IQueryHandler<GetAvailabilitySlotsQuery, IReadOnlyList<AvailabilitySlotDto>>
{
    public async Task<Result<IReadOnlyList<AvailabilitySlotDto>>> Handle(GetAvailabilitySlotsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Booking.Domain.Entities.AvailabilitySlot> slots;

        if (request.TourId.HasValue)
        {
            slots = await slotRepository.GetByTourIdAsync(request.TourId.Value, cancellationToken).ConfigureAwait(false);
        }
        else if (request.TourGuideId.HasValue)
        {
            slots = await slotRepository.GetByTourGuideIdAsync(request.TourGuideId.Value, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return Result.Success<IReadOnlyList<AvailabilitySlotDto>>(Array.Empty<AvailabilitySlotDto>());
        }

        // Apply optional date filter
        var filtered = slots.AsEnumerable();
        if (request.FromDate.HasValue)
        {
            filtered = filtered.Where(s => s.Date >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            filtered = filtered.Where(s => s.Date <= request.ToDate.Value);
        }

        var dtos = filtered.Select(s => new AvailabilitySlotDto(
            s.Id, s.TourGuideId, s.SlotType, s.TourId, s.Date, s.StartTime, s.EndTime,
            s.MaxCapacity, s.BookedCount, s.LockedCount, s.AvailableCount,
            s.IsActive, s.PriceOverride, s.PriceOverrideCurrency))
            .ToList();

        return Result.Success<IReadOnlyList<AvailabilitySlotDto>>(dtos);
    }
}
