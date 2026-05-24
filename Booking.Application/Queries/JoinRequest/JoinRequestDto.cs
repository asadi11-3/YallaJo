using Booking.Domain.Enums;

namespace Booking.Application.Queries.JoinRequest;

public sealed record JoinRequestDto(
    Guid Id,
    Guid TourBookingId,
    Guid UserId,
    int ParticipantCount,
    string? Message,
    JoinRequestStatus Status,
    DateTime? RespondedAt,
    string? ResponseMessage,
    string RowVersion);
