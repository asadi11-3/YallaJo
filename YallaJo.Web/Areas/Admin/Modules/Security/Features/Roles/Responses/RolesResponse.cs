namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Responses;

/// <summary>Mirrors RoleDto from Security.Application.</summary>
public sealed class RoleItemResponse
{
    public Guid    Id          { get; init; }
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool    IsActive    { get; init; }
}

/// <summary>Minimal create-role response (backend returns Id + Name at minimum).</summary>
public sealed class CreateRoleResponse
{
    public Guid   Id   { get; init; }
    public string Name { get; init; } = string.Empty;
}
