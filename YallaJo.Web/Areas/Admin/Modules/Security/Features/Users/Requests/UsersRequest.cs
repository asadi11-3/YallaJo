namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Requests;

/// <summary>Outbound payload for POST /api/v1/security/users/{userId}/roles.</summary>
public sealed class AssignRoleRequest
{
    public Guid RoleId { get; init; }
}

/// <summary>Outbound payload for POST /api/v1/security/users/{userId}/claims.</summary>
public sealed class AddUserClaimRequest
{
    public string ClaimType  { get; init; } = string.Empty;
    public string ClaimValue { get; init; } = string.Empty;
}
