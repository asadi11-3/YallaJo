using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models.AgencyRoster;

/// <summary>Aggregated agency-owner roster overview for GET /guide/agency/roster.</summary>
public sealed class AgencyRosterVm
{
    public IReadOnlyList<AgencyGuideRowVm> Guides { get; init; } = [];
    public IReadOnlyList<AgencyApplicationRowVm> Applications { get; init; } = [];
    public IReadOnlyList<AgencySentInvitationRowVm> SentInvitations { get; init; } = [];

    public bool HasGuides => Guides.Count > 0;
    public bool HasApplications => Applications.Count > 0;
    public bool HasSentInvitations => SentInvitations.Count > 0;

    public int PendingApplicationCount { get; init; }
    public int ActiveGuideCount => Guides.Count;
    public int PendingInvitationCount { get; init; }

    /// <summary>Inline invite-guide form (Phase 3: the Invite page was merged into this view
    /// at the #invite-guide anchor; GET /guide/agency/roster/invite 301s here).</summary>
    public InviteGuideFormVm InviteForm { get; set; } = new();
}

public sealed record AgencyGuideRowVm(
    Guid AffiliationId,
    Guid GuideUserId,
    string GuideHandle,
    decimal CommissionPercentage,
    DateTime JoinedAt);

public sealed record AgencyApplicationRowVm(
    Guid Id,
    Guid GuideUserId,
    string GuideHandle,
    string? Message,
    AgencyApplicationStatus Status,
    string StatusBadgeClass,
    string? RejectionReason,
    DateTime? ReviewedAt,
    DateTime CreatedAt)
{
    public bool IsPending => Status == AgencyApplicationStatus.Pending;
}

public sealed record AgencySentInvitationRowVm(
    Guid Id,
    Guid GuideUserId,
    string GuideHandle,
    string? Message,
    decimal ProposedCommissionPercentage,
    RosterInvitationStatus Status,
    string StatusBadgeClass,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    DateTime CreatedAt);

/// <summary>Invite-guide form (GET/POST /guide/agency/roster/invite). Validation mirrors
/// the backend InviteGuideCommandValidator (commission 0–100, message ≤1000).</summary>
public sealed class InviteGuideFormVm
{
    [Required(ErrorMessage = "Please choose a guide to invite.")]
    [Display(Name = "Guide")]
    public Guid GuideUserId { get; set; }

    [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100.")]
    [Display(Name = "Proposed commission (%)")]
    public decimal ProposedCommissionPercentage { get; set; } = 20m;

    [StringLength(1000, ErrorMessage = "Message must not exceed 1000 characters.")]
    [Display(Name = "Message (optional)")]
    public string? Message { get; set; }

    /// <summary>Available independent guides to pick from (populated for rendering).</summary>
    public IReadOnlyList<AvailableGuideOptionVm> AvailableGuides { get; set; } = [];

    public bool HasAvailableGuides => AvailableGuides.Count > 0;
}

public sealed record AvailableGuideOptionVm(Guid UserId, string BusinessName, string ContactEmail);
