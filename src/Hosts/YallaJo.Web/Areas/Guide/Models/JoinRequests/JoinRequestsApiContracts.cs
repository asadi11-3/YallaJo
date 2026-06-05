namespace YallaJo.Web.Areas.Guide.Models.JoinRequests;

/// <summary>
/// Status of a join request, mirroring the backend Booking domain enum
/// (Booking.Domain.Enums.JoinRequestStatus). Serialized as a numeric byte.
/// </summary>
public enum JoinRequestStatus : byte
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Expired = 4
}

/// <summary>
/// A join request as returned by GET /api/v1/booking/join-requests.
/// </summary>
public sealed record JoinRequestResponse(
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

/// <summary>
/// Optional response message sent when approving a join request.
/// POST /api/v1/booking/join-requests/{id}/approve
/// </summary>
public sealed record ApproveJoinRequestRequest(string? ResponseMessage);

/// <summary>
/// Optional response message sent when rejecting a join request.
/// POST /api/v1/booking/join-requests/{id}/reject
/// </summary>
public sealed record RejectJoinRequestRequest(string? ResponseMessage);
