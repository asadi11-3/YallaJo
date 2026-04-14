namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Requests;

/// <summary>Outbound payload for POST /api/v1/security/roles.</summary>
public sealed class CreateRoleRequest
{
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
}

/// <summary>Outbound payload for PATCH /api/v1/security/roles/{roleId}.</summary>
public sealed class UpdateRoleRequest
{
    public string? Description { get; init; }
}

/// <summary>Outbound payload for POST /api/v1/security/roles/{roleId}/claims.</summary>
public sealed class AddRoleClaimRequest
{
    public string ClaimType  { get; init; } = string.Empty;
    public string ClaimValue { get; init; } = string.Empty;
}
