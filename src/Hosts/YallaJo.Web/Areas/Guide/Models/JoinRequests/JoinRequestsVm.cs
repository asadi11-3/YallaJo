using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models.JoinRequests;

/// <summary>
/// View model backing the guide "Join Requests" page.
/// </summary>
public sealed class JoinRequestsVm
{
    public IReadOnlyList<JoinRequestRowVm> Requests { get; init; } = [];

    public bool HasRequests => Requests.Count > 0;

    public int PendingCount => Requests.Count(r => r.Status == JoinRequestStatus.Pending);
}

/// <summary>
/// A single join request row.
/// </summary>
public sealed record JoinRequestRowVm(
    Guid Id,
    Guid TourBookingId,
    JoinRequestStatus Status,
    int ParticipantCount,
    string? Message,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    string? ResponseMessage,
    DateTime CreatedAt)
{
    public bool IsPending => Status == JoinRequestStatus.Pending;

    public bool IsExpired => ExpiresAt <= DateTime.UtcNow && Status == JoinRequestStatus.Pending;
}

/// <summary>
/// Form used by the guide to approve or reject a join request.
/// </summary>
public sealed class RespondToJoinRequestFormVm
{
    [Required]
    public Guid RequestId { get; set; }

    [StringLength(1000)]
    [Display(Name = "Response message (optional)")]
    public string? ResponseMessage { get; set; }
}
