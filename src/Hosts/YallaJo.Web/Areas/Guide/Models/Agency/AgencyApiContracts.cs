namespace YallaJo.Web.Areas.Guide.Models.Agency;

public enum AgencyInvitationStatus : byte
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3,
}

public sealed record AgencyInvitationResponse(
    Guid Id,
    Guid AgencyUserId,
    Guid GuideUserId,
    string? Message,
    decimal ProposedCommissionPercentage,
    AgencyInvitationStatus Status,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    DateTime CreatedAt);

public sealed record ApplyToAgencyRequest(string? Message);

/// <summary>GET /api/v1/agency — mirrors Accounts GetAgenciesResult.</summary>
public sealed record GetAgenciesResponse(IReadOnlyList<AgencyOptionResponse> Agencies, int TotalCount);

/// <summary>Mirrors AgencyListItemDto; UserId is the agencyUserId the apply route expects.</summary>
public sealed record AgencyOptionResponse(Guid UserId, string BusinessName, string? ContactEmail);
