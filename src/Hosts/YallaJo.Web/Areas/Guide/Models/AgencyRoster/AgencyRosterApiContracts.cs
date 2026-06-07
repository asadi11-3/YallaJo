namespace YallaJo.Web.Areas.Guide.Models.AgencyRoster;

// Agency-side roster status enums (mirror Accounts.Domain.Enums).

public enum AgencyApplicationStatus : byte
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

public enum RosterInvitationStatus : byte
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3,
}

// ── Responses (mirror /api/v1/agency/* DTOs) ───────────────────────────────────

/// <summary>Mirrors AgencyGuideDto from GET /api/v1/agency/guides.</summary>
public sealed record AgencyGuideResponse(
    Guid AffiliationId,
    Guid GuideUserId,
    decimal CommissionPercentage,
    DateTime JoinedAt);

/// <summary>Mirrors AgencyApplicationDto from GET /api/v1/agency/applications.</summary>
public sealed record AgencyApplicationResponse(
    Guid Id,
    Guid GuideUserId,
    Guid AgencyUserId,
    string? Message,
    AgencyApplicationStatus Status,
    string? RejectionReason,
    DateTime? ReviewedAt,
    DateTime CreatedAt);

/// <summary>Mirrors AvailableGuideDto from GET /api/v1/agency/guides/available.</summary>
public sealed record AvailableGuideResponse(
    Guid UserId,
    string BusinessName,
    string ContactEmail,
    DateTime CreatedAt);

/// <summary>Mirrors InvitationDto from GET /api/v1/agency/invitations/sent.</summary>
public sealed record AgencySentInvitationResponse(
    Guid Id,
    Guid AgencyUserId,
    Guid GuideUserId,
    string? Message,
    decimal ProposedCommissionPercentage,
    RosterInvitationStatus Status,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    DateTime CreatedAt);

// ── Requests (to /api/v1/agency/*) ─────────────────────────────────────────────

public sealed record InviteGuideApiRequest(
    Guid GuideUserId,
    string? Message,
    decimal ProposedCommissionPercentage);

public sealed record RejectAgencyApplicationApiRequest(string Reason);

public sealed record RemoveAgencyGuideApiRequest(string Reason);
