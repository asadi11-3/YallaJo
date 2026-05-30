using Booking.Domain.Enums;

namespace Booking.Application.Queries.GetJoinRequests;

public sealed record JoinRequestDto(
    Guid Id,
    Guid TourBookingId,
    Guid UserId,
    JoinRequestStatus Status,
    int ParticipantCount,
    string? Message,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    string? ResponseMessage,
    Guid? ResultingBookingId,
    DateTime CreatedAt);
