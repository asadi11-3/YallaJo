using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Application.Queries.GetJoinRequests;

/// <summary>Gets join requests for a booking (for guide/provider review) or for the current user.</summary>
public sealed record GetJoinRequestsQuery(
    Guid? TourBookingId,
    bool MyRequestsOnly = false)
    : IQuery<IReadOnlyList<JoinRequestDto>>;
