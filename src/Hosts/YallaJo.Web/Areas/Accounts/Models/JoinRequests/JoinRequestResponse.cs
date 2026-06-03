namespace YallaJo.Web.Areas.Accounts.Models.JoinRequests;

/// <summary>
/// API response for a single group-booking join request
/// (GET /api/v1/booking/join-requests?myRequestsOnly=true).
/// </summary>
public sealed class JoinRequestResponse
{
    public Guid Id { get; init; }
    public Guid TourBookingId { get; init; }
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
    public string? Message { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public string? ResponseMessage { get; init; }
    public Guid? ResultingBookingId { get; init; }
    public DateTime CreatedAt { get; init; }
}
