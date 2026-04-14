namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Requests;

public sealed class CreateRoleRequest
{
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
}
