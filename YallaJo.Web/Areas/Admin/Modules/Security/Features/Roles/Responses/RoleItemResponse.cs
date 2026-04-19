namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Responses;

public sealed class RoleItemResponse
{
    public Guid    Id          { get; init; }
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool    IsActive    { get; init; }
}
