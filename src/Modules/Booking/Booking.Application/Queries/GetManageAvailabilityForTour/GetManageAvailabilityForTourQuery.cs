using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetManageAvailabilityForTour;

/// <summary>
/// Owner-scoped: lists ALL availability slots (active and inactive) for a tour the
/// caller provider owns. Backs the provider availability-management screen.
/// </summary>
public sealed record GetManageAvailabilityForTourQuery(Guid TourId)
    : IQuery<IReadOnlyList<ManageAvailabilitySlotDto>>;
