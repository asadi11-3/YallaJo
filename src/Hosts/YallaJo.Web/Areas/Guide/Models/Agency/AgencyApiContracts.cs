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
