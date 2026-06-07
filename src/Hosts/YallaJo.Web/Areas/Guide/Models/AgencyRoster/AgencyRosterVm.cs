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
